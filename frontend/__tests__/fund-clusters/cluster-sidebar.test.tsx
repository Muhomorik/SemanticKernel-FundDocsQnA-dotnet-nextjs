import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  ClusterSidebar,
  ClusterSidebarProps,
} from "@/components/fund-clusters/cluster-sidebar";
import { FUNDS, METHOD } from "@/test-utils/fund-clusters";

function setup(overrides: Partial<ClusterSidebarProps> = {}) {
  const props: ClusterSidebarProps = {
    doc: METHOD,
    funds: FUNDS,
    tab: "categories",
    onTabChange: jest.fn(),
    isolate: null,
    onIsolate: jest.fn(),
    pinned: null,
    onPin: jest.fn(),
    ...overrides,
  };
  render(<ClusterSidebar {...props} />);
  return props;
}

function rows() {
  return within(screen.getByRole("tabpanel")).getAllByRole("button");
}

describe("ClusterSidebar tabs", () => {
  it("marks the selected tab and reports a change", async () => {
    const props = setup();

    expect(screen.getByRole("tab", { name: "Categories" })).toHaveAttribute(
      "aria-selected",
      "true"
    );
    await userEvent.click(screen.getByRole("tab", { name: "Clusters" }));

    expect(props.onTabChange).toHaveBeenCalledWith("clusters");
  });

  it("lists categories with their agreement share", () => {
    setup();

    expect(rows()).toHaveLength(4);
    expect(
      screen.getByRole("button", { name: /USA, Mix bolag/ })
    ).toHaveTextContent("100%");
    expect(screen.getByRole("button", { name: /^Sverige/ })).toHaveTextContent(
      "–"
    );
  });

  it("isolates a category on click and clears it on a second click", async () => {
    const props = setup({ isolate: { type: "category", value: "Norden" } });

    const norden = screen.getByRole("button", { name: /^Norden/ });
    expect(norden).toHaveAttribute("aria-pressed", "true");
    await userEvent.click(norden);
    await userEvent.click(screen.getByRole("button", { name: /^Sverige/ }));

    expect(props.onIsolate).toHaveBeenNthCalledWith(1, null);
    expect(props.onIsolate).toHaveBeenNthCalledWith(2, {
      type: "category",
      value: "Sverige",
    });
  });

  it("lists clusters by size with their composition", async () => {
    const props = setup({ tab: "clusters" });

    expect(rows()[0]).toHaveTextContent("USA, Mix bolag");
    expect(rows()[0]).toHaveTextContent("50% pure");
    await userEvent.click(rows()[1]);

    expect(props.onIsolate).toHaveBeenCalledWith({ type: "cluster", value: 1 });
  });

  it("lists disagreements, different family first, and pins on click", async () => {
    const props = setup({ tab: "disagreements" });

    expect(rows().map((r) => r.textContent)).toEqual([
      expect.stringContaining("Example Tech Fund"),
      expect.stringContaining("Example Nordic Fund"),
    ]);
    await userEvent.click(rows()[1]);

    expect(props.onPin).toHaveBeenCalledWith("SE0000000002");
  });
});

describe("ClusterSidebar pinned fund", () => {
  it("shows nothing when no fund is pinned", () => {
    setup();

    expect(
      screen.queryByRole("heading", { name: /example/i })
    ).not.toBeInTheDocument();
  });

  it("shows the fund, its cluster and verdict", () => {
    setup({ pinned: "SE0000000003" });

    const card = screen.getByRole("region", { name: "Example Tech Fund" });
    expect(card).toHaveTextContent("Branschfond, Ny teknik");
    expect(card).toHaveTextContent("USA, Mix bolag (2 funds, 50% pure)");
    expect(card).toHaveTextContent("Different family");
    expect(card).toHaveTextContent("Few close neighbours");
    expect(card).toHaveTextContent("Teknik 90.0%");
  });

  it("lists similar funds and re-pins on click", async () => {
    const props = setup({ pinned: "SE0000000001" });

    const similar = within(screen.getByRole("list", { name: /similar/i }));
    const items = similar.getAllByRole("button");
    expect(items[0]).toHaveTextContent("Example Tech Fund");
    expect(items[0]).toHaveTextContent("0.031");
    await userEvent.click(items[1]);

    expect(props.onPin).toHaveBeenCalledWith("SE0000000002");
  });

  it("clears the pin", async () => {
    const props = setup({ pinned: "SE0000000001" });

    await userEvent.click(
      screen.getByRole("button", { name: /clear selection/i })
    );

    expect(props.onPin).toHaveBeenCalledWith(null);
  });

  it("says when a fund is not in a cluster", () => {
    setup({ pinned: "SE0000000004" });

    expect(
      screen.getByRole("region", { name: "Example Sverige Fund" })
    ).toHaveTextContent("Not in a cluster");
  });
});
