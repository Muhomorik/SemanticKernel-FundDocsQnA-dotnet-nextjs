import { ApiError } from "@/lib/api";
import {
  fetchOwnershipFlow,
  fetchOwnershipPeriods,
  OwnershipFlowResponse,
  OwnershipPeriodsResponse,
} from "@/lib/ownership-flow";

// ── Fixtures ─────────────────────────────────────────────────────────────────

const MOCK_PERIODS: OwnershipPeriodsResponse = {
  weekly: [
    { label: "Feb 10 – 16", from: "2025-02-10", to: "2025-02-16" },
    { label: "Feb 17 – 23", from: "2025-02-17", to: "2025-02-23" },
  ],
  monthly: [
    { label: "1 month", from: "2025-01-10", to: "2025-02-10" },
  ],
};

const MOCK_FLOW: OwnershipFlowResponse = {
  periodLabel: "Feb 10 – 16",
  cat: {
    out: [{ name: "Sverige", value: 2217, pct: -1.9 }],
    in: [{ name: "Global", value: 3147, pct: 1.2 }],
  },
  fund: {
    out: [{ name: "Swedbank Robur Sverige", value: 831, pct: -1.8 }],
    in: [{ name: "Avanza Zero", value: 1245, pct: 0.9 }],
  },
};

// ── Setup ─────────────────────────────────────────────────────────────────────

beforeEach(() => {
  jest.resetAllMocks();
});

// ── fetchOwnershipPeriods ─────────────────────────────────────────────────────

describe("fetchOwnershipPeriods", () => {
  it("returns typed data on 200", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(MOCK_PERIODS),
    });

    const result = await fetchOwnershipPeriods();

    expect(result).toEqual(MOCK_PERIODS);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/ownership-flow/periods"),
    );
  });

  it("throws ApiError on non-ok response", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 503,
      statusText: "Service Unavailable",
      text: () => Promise.resolve("Azure SQL not configured"),
    });

    await expect(fetchOwnershipPeriods()).rejects.toThrow(ApiError);
  });

  it("throws ApiError with status code on non-ok response", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 503,
      statusText: "Service Unavailable",
      text: () => Promise.resolve(""),
    });

    try {
      await fetchOwnershipPeriods();
      fail("should have thrown");
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError);
      expect((e as ApiError).statusCode).toBe(503);
    }
  });

  it("throws ApiError on network failure", async () => {
    global.fetch = jest.fn().mockRejectedValue(new Error("Network error"));

    await expect(fetchOwnershipPeriods()).rejects.toThrow(ApiError);
  });

  it("wraps network error message", async () => {
    global.fetch = jest.fn().mockRejectedValue(new Error("Failed to fetch"));

    try {
      await fetchOwnershipPeriods();
      fail("should have thrown");
    } catch (e) {
      expect((e as ApiError).message).toBe("Failed to fetch");
    }
  });
});

// ── fetchOwnershipFlow ────────────────────────────────────────────────────────

describe("fetchOwnershipFlow", () => {
  it("returns typed data on 200", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(MOCK_FLOW),
    });

    const result = await fetchOwnershipFlow("2025-02-10", "2025-02-16");

    expect(result).toEqual(MOCK_FLOW);
  });

  it("includes from and to as query parameters in the URL", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(MOCK_FLOW),
    });

    await fetchOwnershipFlow("2025-02-10", "2025-02-16");

    const calledUrl = (global.fetch as jest.Mock).mock.calls[0][0] as string;
    expect(calledUrl).toContain("from=2025-02-10");
    expect(calledUrl).toContain("to=2025-02-16");
    expect(calledUrl).toContain("/api/ownership-flow");
  });

  it("throws ApiError on 400 response", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 400,
      statusText: "Bad Request",
      text: () => Promise.resolve("from must be earlier than to"),
    });

    await expect(fetchOwnershipFlow("2025-02-16", "2025-02-10")).rejects.toThrow(ApiError);
  });

  it("throws ApiError with correct status code", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 500,
      statusText: "Internal Server Error",
      text: () => Promise.resolve(""),
    });

    try {
      await fetchOwnershipFlow("2025-02-10", "2025-02-16");
      fail("should have thrown");
    } catch (e) {
      expect((e as ApiError).statusCode).toBe(500);
    }
  });

  it("throws ApiError on network failure", async () => {
    global.fetch = jest.fn().mockRejectedValue(new TypeError("Failed to fetch"));

    await expect(fetchOwnershipFlow("2025-02-10", "2025-02-16")).rejects.toThrow(ApiError);
  });
});
