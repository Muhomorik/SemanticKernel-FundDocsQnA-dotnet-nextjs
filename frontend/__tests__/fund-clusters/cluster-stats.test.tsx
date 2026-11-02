import { render, screen } from "@testing-library/react";
import { ClusterStats } from "@/components/fund-clusters/cluster-stats";
import { METHOD } from "@/test-utils/fund-clusters";

describe("ClusterStats", () => {
  it("shows the verdict shares as whole percentages", () => {
    render(<ClusterStats doc={METHOD} />);

    expect(screen.getByText("Agree").previousSibling).toHaveTextContent("33%");
    expect(screen.getByText("Neighbouring").previousSibling).toHaveTextContent(
      "33%"
    );
    expect(
      screen.getByText("Different family").previousSibling
    ).toHaveTextContent("33%");
  });

  it("shows funds outside clusters and the agreement scores", () => {
    render(<ClusterStats doc={METHOD} />);

    expect(screen.getByText("No cluster").previousSibling).toHaveTextContent(
      "1"
    );
    expect(screen.getByText("ARI").previousSibling).toHaveTextContent("0.40");
    expect(screen.getByText("NMI").previousSibling).toHaveTextContent("0.76");
  });

  it("shows a dash for missing scores", () => {
    render(
      <ClusterStats
        doc={{
          ...METHOD,
          summary: { ...METHOD.summary, ari: null, nmi: null },
        }}
      />
    );

    expect(screen.getByText("ARI").previousSibling).toHaveTextContent("–");
  });

  it("explains the method on hover", () => {
    render(<ClusterStats doc={METHOD} />);

    expect(screen.getByRole("list")).toHaveAttribute(
      "title",
      METHOD.description
    );
  });
});
