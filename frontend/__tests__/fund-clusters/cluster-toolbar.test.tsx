import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  ClusterToolbar,
  ClusterToolbarProps,
} from "@/components/fund-clusters/cluster-toolbar";
import { FUNDS, MANIFEST } from "@/test-utils/fund-clusters";

function setup(overrides: Partial<ClusterToolbarProps> = {}) {
  const props: ClusterToolbarProps = {
    methods: MANIFEST.methods,
    methodId: "cosine",
    onMethodChange: jest.fn(),
    funds: FUNDS,
    onPickFund: jest.fn(),
    view: "all",
    onViewChange: jest.fn(),
    layers: { territory: true, names: true, rings: true },
    onLayersChange: jest.fn(),
    onResetZoom: jest.fn(),
    ...overrides,
  };
  render(<ClusterToolbar {...props} />);
  return props;
}

describe("ClusterToolbar", () => {
  it("lists the methods from the manifest", () => {
    setup();

    const select = screen.getByRole("combobox", { name: /method/i });
    expect(select).toHaveValue("cosine");
    expect(
      screen.getByRole("option", { name: "Aitchison" })
    ).toBeInTheDocument();
  });

  it("reports a method change", async () => {
    const props = setup();

    await userEvent.selectOptions(
      screen.getByRole("combobox", { name: /method/i }),
      "aitchison"
    );

    expect(props.onMethodChange).toHaveBeenCalledWith("aitchison");
  });

  it("marks the current view as pressed and reports a change", async () => {
    const props = setup();

    expect(screen.getByRole("button", { name: "All funds" })).toHaveAttribute(
      "aria-pressed",
      "true"
    );
    await userEvent.click(
      screen.getByRole("button", { name: "Disagreements" })
    );

    expect(props.onViewChange).toHaveBeenCalledWith("disagree");
  });

  it("toggles a map layer", async () => {
    const props = setup();

    await userEvent.click(
      screen.getByRole("checkbox", { name: /cluster names/i })
    );

    expect(props.onLayersChange).toHaveBeenCalledWith({
      territory: true,
      names: false,
      rings: true,
    });
  });

  it("resets the zoom", async () => {
    const props = setup();

    await userEvent.click(screen.getByRole("button", { name: /reset zoom/i }));

    expect(props.onResetZoom).toHaveBeenCalled();
  });

  it("pins a fund found by search", async () => {
    const props = setup();

    await userEvent.type(
      screen.getByRole("combobox", { name: /find a fund/i }),
      "tech{Enter}"
    );

    expect(props.onPickFund).toHaveBeenCalledWith("SE0000000003");
  });
});
