import { render, screen } from "@testing-library/react";
import { SankeyEmpty } from "@/components/ownership-flow/sankey-empty";

describe("SankeyEmpty", () => {
  it('shows "no changes" message for variant=none', () => {
    render(<SankeyEmpty variant="none" />);
    expect(screen.getByText(/No ownership changes for this period/i)).toBeInTheDocument();
  });

  it('shows "only inflows" message for variant=only-in', () => {
    render(<SankeyEmpty variant="only-in" />);
    expect(screen.getByText(/Only inflows this period/i)).toBeInTheDocument();
  });

  it('shows "only outflows" message for variant=only-out', () => {
    render(<SankeyEmpty variant="only-out" />);
    expect(screen.getByText(/Only outflows this period/i)).toBeInTheDocument();
  });
});
