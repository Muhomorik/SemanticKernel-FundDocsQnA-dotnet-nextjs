import { render, screen } from "@testing-library/react";
import { OwnershipFlowPage } from "@/components/ownership-flow/ownership-flow-page";
import {
  fetchOwnershipFlow,
  fetchOwnershipPeriods,
  OwnershipPeriodsResponse,
} from "@/lib/ownership-flow";

jest.mock("@/lib/ownership-flow", () => ({
  ...jest.requireActual("@/lib/ownership-flow"),
  fetchOwnershipPeriods: jest.fn(),
  fetchOwnershipFlow: jest.fn(),
}));

const mockedFetchPeriods = fetchOwnershipPeriods as jest.MockedFunction<
  typeof fetchOwnershipPeriods
>;
const mockedFetchFlow = fetchOwnershipFlow as jest.MockedFunction<
  typeof fetchOwnershipFlow
>;

const PERIODS: OwnershipPeriodsResponse = {
  weekly: [
    { label: "Feb 10 – 16", from: "2025-02-10", to: "2025-02-16" },
    { label: "Feb 17 – 23", from: "2025-02-17", to: "2025-02-23" },
  ],
  monthly: [{ label: "1 month", from: "2025-01-10", to: "2025-02-10" }],
};

describe("OwnershipFlowPage", () => {
  beforeEach(() => {
    // Flow data never resolves — these tests cover period loading only
    mockedFetchFlow.mockReturnValue(new Promise(() => {}));
  });

  afterEach(() => {
    jest.resetAllMocks();
  });

  it("fetches periods once on mount", async () => {
    mockedFetchPeriods.mockResolvedValue(PERIODS);

    render(<OwnershipFlowPage />);

    await screen.findByText("1 month");
    expect(mockedFetchPeriods).toHaveBeenCalledTimes(1);
  });

  it("selects the most recent monthly period by default", async () => {
    mockedFetchPeriods.mockResolvedValue(PERIODS);

    render(<OwnershipFlowPage />);

    await screen.findByText("1 month");
    expect(mockedFetchFlow).toHaveBeenCalledWith("2025-01-10", "2025-02-10");
  });

  it("falls back to the latest weekly period when there are no monthly periods", async () => {
    mockedFetchPeriods.mockResolvedValue({ ...PERIODS, monthly: [] });

    render(<OwnershipFlowPage />);

    await screen.findByText("Feb 17 – 23");
    expect(mockedFetchFlow).toHaveBeenCalledWith("2025-02-17", "2025-02-23");
  });

  it("shows an error when periods fail to load", async () => {
    mockedFetchPeriods.mockRejectedValue(new Error("Backend down"));

    render(<OwnershipFlowPage />);

    expect(
      await screen.findByText(/Could not load time periods: Backend down/)
    ).toBeInTheDocument();
    expect(mockedFetchFlow).not.toHaveBeenCalled();
  });
});
