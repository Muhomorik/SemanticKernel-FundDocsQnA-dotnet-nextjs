import { useImperativeHandle } from "react";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FundClustersPage } from "@/components/fund-clusters/fund-clusters-page";
import type { ClusterMapProps } from "@/components/fund-clusters/cluster-map";
import {
  fetchClusterFunds,
  fetchClusterMethod,
  fetchClusterMethods,
} from "@/lib/fund-clusters";
import { FUNDS, MANIFEST, METHOD } from "@/test-utils/fund-clusters";

jest.mock("@/lib/fund-clusters", () => ({
  ...jest.requireActual("@/lib/fund-clusters"),
  fetchClusterMethods: jest.fn(),
  fetchClusterFunds: jest.fn(),
  fetchClusterMethod: jest.fn(),
}));

const zoomTo = jest.fn();
const resetZoom = jest.fn();
let mapProps: ClusterMapProps | null = null;

jest.mock("@/components/fund-clusters/cluster-map", () => ({
  ClusterMap: (props: ClusterMapProps) => {
    mapProps = props;
    useImperativeHandle(props.ref, () => ({ zoomTo, resetZoom }));
    return <div data-testid="map">{props.doc.method}</div>;
  },
}));

jest.mock("@/components/demo-banner", () => ({ DemoBanner: () => null }));

const mockMethods = fetchClusterMethods as jest.Mock;
const mockFunds = fetchClusterFunds as jest.Mock;
const mockMethod = fetchClusterMethod as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
  mapProps = null;
  mockMethods.mockResolvedValue(MANIFEST);
  mockFunds.mockResolvedValue({ week: "2026-W44", funds: FUNDS });
  mockMethod.mockImplementation((id: string) =>
    Promise.resolve({ ...METHOD, method: id, name: id })
  );
});

describe("FundClustersPage", () => {
  it("loads the default method and draws it", async () => {
    render(<FundClustersPage />);

    expect(await screen.findByTestId("map")).toHaveTextContent("cosine");
    expect(mockMethod).toHaveBeenCalledWith("cosine", expect.any(AbortSignal));
    expect(
      screen.getByText(/week 2026-W44, 4 equity funds, 2 clusters/i)
    ).toBeInTheDocument();
  });

  it("shows an error when the data cannot be loaded", async () => {
    mockMethods.mockRejectedValue(
      new Error("Fund cluster data is not available")
    );

    render(<FundClustersPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Fund cluster data is not available"
    );
  });

  it("loads another method when it is selected", async () => {
    render(<FundClustersPage />);
    await screen.findByTestId("map");

    await userEvent.selectOptions(
      screen.getByRole("combobox", { name: /method/i }),
      "aitchison"
    );

    await waitFor(() =>
      expect(screen.getByTestId("map")).toHaveTextContent("aitchison")
    );
    expect(mockMethods).toHaveBeenCalledTimes(1);
    expect(mockFunds).toHaveBeenCalledTimes(1);
  });

  it("pins and zooms to a fund picked in search", async () => {
    render(<FundClustersPage />);
    await screen.findByTestId("map");

    await userEvent.type(
      screen.getByRole("combobox", { name: /find a fund/i }),
      "tech{Enter}"
    );

    expect(
      screen.getByRole("region", { name: "Example Tech Fund" })
    ).toBeInTheDocument();
    expect(zoomTo).toHaveBeenCalledWith("SE0000000003");
    expect(mapProps?.pinned).toBe("SE0000000003");
  });

  it("pins without zooming when a dot is clicked", async () => {
    render(<FundClustersPage />);
    await screen.findByTestId("map");

    mapProps!.onPin("SE0000000002");

    expect(
      await screen.findByRole("region", { name: "Example Nordic Fund" })
    ).toBeInTheDocument();
    expect(zoomTo).not.toHaveBeenCalled();
  });

  it("isolates a category from the sidebar", async () => {
    render(<FundClustersPage />);
    await screen.findByTestId("map");

    await userEvent.click(
      within(screen.getByRole("tabpanel")).getByRole("button", {
        name: /^Norden/,
      })
    );

    expect(mapProps?.isolate).toEqual({ type: "category", value: "Norden" });
  });

  it("resets the zoom", async () => {
    render(<FundClustersPage />);
    await screen.findByTestId("map");

    await userEvent.click(screen.getByRole("button", { name: /reset zoom/i }));

    expect(resetZoom).toHaveBeenCalled();
  });

  it("clears the isolation but keeps the pinned fund when switching method", async () => {
    render(<FundClustersPage />);
    await screen.findByTestId("map");
    mapProps!.onPin("SE0000000002");
    await userEvent.click(
      within(screen.getByRole("tabpanel")).getByRole("button", {
        name: /^Norden/,
      })
    );

    await userEvent.selectOptions(
      screen.getByRole("combobox", { name: /method/i }),
      "aitchison"
    );

    await waitFor(() => expect(mapProps?.doc.method).toBe("aitchison"));
    expect(mapProps?.isolate).toBeNull();
    expect(mapProps?.pinned).toBe("SE0000000002");
  });
});
