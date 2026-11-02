import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FundSearch } from "@/components/fund-clusters/fund-search";
import { FUNDS } from "@/test-utils/fund-clusters";

function setup() {
  const onPick = jest.fn();
  render(<FundSearch funds={FUNDS} onPick={onPick} />);
  return { onPick, input: screen.getByRole("combobox") };
}

describe("FundSearch", () => {
  it("lists matches after two characters", async () => {
    const { input } = setup();

    await userEvent.type(input, "no");

    expect(screen.getByRole("listbox")).toBeInTheDocument();
    expect(screen.getAllByRole("option")).toHaveLength(1);
    expect(screen.getByRole("option")).toHaveTextContent("Example Nordic Fund");
    expect(input).toHaveAttribute("aria-expanded", "true");
  });

  it("shows nothing for one character", async () => {
    const { input } = setup();

    await userEvent.type(input, "e");

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(input).toHaveAttribute("aria-expanded", "false");
  });

  it("picks the highlighted match with Enter", async () => {
    const { input, onPick } = setup();

    await userEvent.type(input, "example{ArrowDown}{Enter}");

    expect(onPick).toHaveBeenCalledWith("SE0000000002");
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(input).toHaveValue("Example Nordic Fund");
  });

  it("wraps around with ArrowUp", async () => {
    const { input, onPick } = setup();

    await userEvent.type(input, "example{ArrowUp}{Enter}");

    expect(onPick).toHaveBeenCalledWith("SE0000000004");
  });

  it("picks a match on click", async () => {
    const { input, onPick } = setup();

    await userEvent.type(input, "SE0000000003");
    await userEvent.click(screen.getByRole("option"));

    expect(onPick).toHaveBeenCalledWith("SE0000000003");
  });

  it("closes on Escape", async () => {
    const { input } = setup();

    await userEvent.type(input, "example{Escape}");

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });
});
