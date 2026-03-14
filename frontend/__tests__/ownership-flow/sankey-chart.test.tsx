import { render, screen, fireEvent } from "@testing-library/react";
import { SankeyChart } from "@/components/ownership-flow/sankey-chart";
import { FlowSide, TooltipState } from "@/lib/ownership-flow";

// next-themes mock
jest.mock("next-themes", () => ({
  useTheme: () => ({ resolvedTheme: "light" }),
}));

const FLOW: FlowSide = {
  out: [
    { name: "Sverige", value: 2217, pct: -1.9 },
    { name: "SEK Bonds", value: 1299, pct: -3.8 },
  ],
  in: [
    { name: "Global", value: 3147, pct: 1.2 },
    { name: "USA", value: 532, pct: 1.7 },
  ],
};

describe("SankeyChart", () => {
  it("renders an SVG element", () => {
    const { container } = render(
      <SankeyChart data={FLOW} svgHeight={300} onTooltipChange={() => {}} />
    );
    expect(container.querySelector("svg")).toBeInTheDocument();
  });

  it("renders the correct number of outflow node rects", () => {
    const { container } = render(
      <SankeyChart data={FLOW} svgHeight={300} onTooltipChange={() => {}} />
    );
    // Rects: 2 out + 2 in + 1 hub = 5 total
    const rects = container.querySelectorAll("rect");
    expect(rects.length).toBe(5);
  });

  it("renders link paths for each outflow node", () => {
    const { container } = render(
      <SankeyChart data={FLOW} svgHeight={300} onTooltipChange={() => {}} />
    );
    // All paths in SVG — out links + in links
    const paths = container.querySelectorAll("path");
    expect(paths.length).toBe(FLOW.out.length + FLOW.in.length);
  });

  it("shows SankeyEmpty when both arrays are empty", () => {
    const empty: FlowSide = { out: [], in: [] };
    render(
      <SankeyChart data={empty} svgHeight={300} onTooltipChange={() => {}} />
    );
    expect(screen.getByText(/No ownership changes/i)).toBeInTheDocument();
  });

  it("shows only-in empty state when out array is empty", () => {
    const onlyIn: FlowSide = {
      out: [],
      in: [{ name: "Global", value: 100, pct: 1 }],
    };
    render(
      <SankeyChart data={onlyIn} svgHeight={300} onTooltipChange={() => {}} />
    );
    expect(screen.getByText(/Only inflows this period/i)).toBeInTheDocument();
  });

  it("shows only-out empty state when in array is empty", () => {
    const onlyOut: FlowSide = {
      out: [{ name: "Sverige", value: 100, pct: -1 }],
      in: [],
    };
    render(
      <SankeyChart data={onlyOut} svgHeight={300} onTooltipChange={() => {}} />
    );
    expect(screen.getByText(/Only outflows this period/i)).toBeInTheDocument();
  });

  it("calls onTooltipChange with side=out on outflow link mouseenter", () => {
    const onChange = jest.fn();
    const { container } = render(
      <SankeyChart data={FLOW} svgHeight={300} onTooltipChange={onChange} />
    );
    const firstPath = container.querySelector("path")!;
    fireEvent.mouseEnter(firstPath, { clientX: 100, clientY: 200 });
    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({ visible: true, side: "out", name: "Sverige" })
    );
  });

  it("calls onTooltipChange with visible=false on mouseleave", () => {
    const onChange = jest.fn();
    const { container } = render(
      <SankeyChart data={FLOW} svgHeight={300} onTooltipChange={onChange} />
    );
    const firstPath = container.querySelector("path")!;
    fireEvent.mouseLeave(firstPath);
    // mouseleave passes an updater function
    const call = onChange.mock.calls[0][0];
    if (typeof call === "function") {
      const prev: TooltipState = {
        visible: true,
        x: 0,
        y: 0,
        name: "",
        value: 0,
        side: "out",
      };
      expect(call(prev)).toMatchObject({ visible: false });
    } else {
      expect(call).toMatchObject({ visible: false });
    }
  });

  it("calls onTooltipChange with side=in on inflow link mouseenter", () => {
    const onChange = jest.fn();
    const { container } = render(
      <SankeyChart data={FLOW} svgHeight={300} onTooltipChange={onChange} />
    );
    // In paths come after out paths
    const paths = container.querySelectorAll("path");
    const firstInPath = paths[FLOW.out.length]; // first inflow link
    fireEvent.mouseEnter(firstInPath, { clientX: 100, clientY: 200 });
    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({ visible: true, side: "in", name: "Global" })
    );
  });
});
