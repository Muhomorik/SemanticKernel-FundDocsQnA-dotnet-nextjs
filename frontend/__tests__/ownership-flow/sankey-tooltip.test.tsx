import { render, screen } from "@testing-library/react";
import { SankeyTooltip } from "@/components/ownership-flow/sankey-tooltip";
import { TooltipState } from "@/lib/ownership-flow";

const STATE: TooltipState = {
  visible: true,
  x: 100,
  y: 50,
  name: "Example Index Fund",
  value: 1234,
  side: "out",
};

describe("SankeyTooltip", () => {
  it("renders into document.body via a portal", () => {
    const { container } = render(
      <div>
        <SankeyTooltip state={STATE} />
      </div>
    );

    const tooltip = screen.getByRole("tooltip");
    expect(tooltip.parentElement).toBe(document.body);
    expect(container).not.toContainElement(tooltip);
  });

  it("shows investors who left for an outflow", () => {
    render(<SankeyTooltip state={STATE} />);

    expect(screen.getByText("Example Index Fund")).toBeInTheDocument();
    expect(screen.getByText("1,234 investors left")).toBeInTheDocument();
  });

  it("shows investors who joined for an inflow", () => {
    render(<SankeyTooltip state={{ ...STATE, side: "in" }} />);

    expect(screen.getByText("1,234 investors joined")).toBeInTheDocument();
  });

  it("is transparent when not visible", () => {
    render(<SankeyTooltip state={{ ...STATE, visible: false }} />);

    expect(screen.getByRole("tooltip")).toHaveStyle({ opacity: "0" });
  });
});
