import { render, screen } from "@testing-library/react";
import { SankeyCard } from "@/components/ownership-flow/sankey-card";
import { FlowSide } from "@/lib/ownership-flow";

// Mock SankeyChart to keep tests focused on SankeyCard logic
jest.mock("@/components/ownership-flow/sankey-chart", () => ({
  SankeyChart: ({ data }: { data: FlowSide }) => (
    <div data-testid="sankey-chart" data-out={data.out.length} data-in={data.in.length} />
  ),
}));

const FLOW_DATA: FlowSide = {
  out: [
    { name: "Sverige", value: 2217, pct: -1.9 },
    { name: "SEK Bonds", value: 1299, pct: -3.8 },
  ],
  in: [
    { name: "Global", value: 3147, pct: 1.2 },
    { name: "USA", value: 532, pct: 1.7 },
    { name: "Mixed", value: 149, pct: 0.6 },
  ],
};

const NOOP = () => {};

describe("SankeyCard", () => {
  it("renders the card title", () => {
    render(
      <SankeyCard
        title="Category overview"
        subtitle="By category"
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={false}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByText("Category overview")).toBeInTheDocument();
  });

  it("renders the subtitle", () => {
    render(
      <SankeyCard
        title="Top funds"
        subtitle="Top 10 funds"
        data={FLOW_DATA}
        svgHeight={520}
        isLoading={false}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByText("Top 10 funds")).toBeInTheDocument();
  });

  it("shows period label badge when provided", () => {
    render(
      <SankeyCard
        title="Category overview"
        subtitle=""
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={false}
        periodLabel="Feb 10 – 16"
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByText("Feb 10 – 16")).toBeInTheDocument();
  });

  it("does not show period label badge when not provided", () => {
    render(
      <SankeyCard
        title="Category overview"
        subtitle=""
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={false}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.queryByText(/Feb/)).not.toBeInTheDocument();
  });

  it("shows outflow count badge", () => {
    render(
      <SankeyCard
        title="Title"
        subtitle=""
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={false}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByText("2 outflows")).toBeInTheDocument();
  });

  it("uses singular 'outflow' for count=1", () => {
    const singleOut: FlowSide = { out: [{ name: "Sverige", value: 100, pct: -1 }], in: FLOW_DATA.in };
    render(
      <SankeyCard title="" subtitle="" data={singleOut} svgHeight={300} isLoading={false} onTooltipChange={NOOP} />,
    );
    expect(screen.getByText("1 outflow")).toBeInTheDocument();
  });

  it("shows inflow count badge", () => {
    render(
      <SankeyCard
        title="Title"
        subtitle=""
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={false}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByText("3 inflows")).toBeInTheDocument();
  });

  it("shows loading skeleton when isLoading=true", () => {
    render(
      <SankeyCard
        title="Title"
        subtitle=""
        data={undefined}
        svgHeight={300}
        isLoading={true}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByLabelText("Loading chart")).toBeInTheDocument();
    expect(screen.queryByTestId("sankey-chart")).not.toBeInTheDocument();
  });

  it("skeleton has the expected height", () => {
    render(
      <SankeyCard
        title="Title"
        subtitle=""
        data={undefined}
        svgHeight={520}
        isLoading={true}
        onTooltipChange={NOOP}
      />,
    );
    const skeleton = screen.getByLabelText("Loading chart");
    expect(skeleton).toHaveStyle({ height: "520px" });
  });

  it("renders SankeyChart when data is present and not loading", () => {
    render(
      <SankeyCard
        title="Title"
        subtitle=""
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={false}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.getByTestId("sankey-chart")).toBeInTheDocument();
  });

  it("does not render SankeyChart when loading", () => {
    render(
      <SankeyCard
        title="Title"
        subtitle=""
        data={FLOW_DATA}
        svgHeight={300}
        isLoading={true}
        onTooltipChange={NOOP}
      />,
    );
    expect(screen.queryByTestId("sankey-chart")).not.toBeInTheDocument();
  });
});
