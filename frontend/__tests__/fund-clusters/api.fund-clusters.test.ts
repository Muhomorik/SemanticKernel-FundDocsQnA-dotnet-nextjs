import { ApiError } from "@/lib/api";
import {
  fetchClusterFunds,
  fetchClusterMethod,
  fetchClusterMethods,
  MethodsManifest,
} from "@/lib/fund-clusters";

const MANIFEST: MethodsManifest = {
  week: "2026-W44",
  default: "cosine",
  methods: [{ id: "cosine", name: "Cosine", file: "cosine.json" }],
};

function okResponse(body: unknown) {
  return { ok: true, json: () => Promise.resolve(body) };
}

beforeEach(() => {
  jest.resetAllMocks();
});

describe("fetchClusterMethods", () => {
  it("fetches the manifest", async () => {
    global.fetch = jest.fn().mockResolvedValue(okResponse(MANIFEST));

    const result = await fetchClusterMethods();

    expect(result).toEqual(MANIFEST);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringMatching(/\/api\/fund-clusters\/methods$/),
      expect.anything()
    );
  });

  it("throws ApiError with status on non-ok response", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 503,
      statusText: "Service Unavailable",
      text: () => Promise.resolve("no data"),
    });

    const error = await fetchClusterMethods().catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).statusCode).toBe(503);
  });

  it("wraps network errors in ApiError", async () => {
    global.fetch = jest.fn().mockRejectedValue(new Error("Failed to fetch"));

    const error = await fetchClusterMethods().catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).message).toBe("Failed to fetch");
  });

  it("passes the abort signal", async () => {
    global.fetch = jest.fn().mockResolvedValue(okResponse(MANIFEST));
    const controller = new AbortController();

    await fetchClusterMethods(controller.signal);

    expect(global.fetch).toHaveBeenCalledWith(expect.any(String), {
      signal: controller.signal,
    });
  });
});

describe("fetchClusterFunds", () => {
  it("fetches funds.json from the funds endpoint", async () => {
    global.fetch = jest
      .fn()
      .mockResolvedValue(okResponse({ week: "2026-W44", funds: [] }));

    const result = await fetchClusterFunds();

    expect(result.funds).toEqual([]);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringMatching(/\/api\/fund-clusters\/funds$/),
      expect.anything()
    );
  });
});

describe("fetchClusterMethod", () => {
  it("fetches the method file by id", async () => {
    global.fetch = jest
      .fn()
      .mockResolvedValue(okResponse({ method: "cosine" }));

    await fetchClusterMethod("cosine");

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringMatching(/\/api\/fund-clusters\/methods\/cosine$/),
      expect.anything()
    );
  });

  it("encodes the id", async () => {
    global.fetch = jest.fn().mockResolvedValue(okResponse({}));

    await fetchClusterMethod("a/b");

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringMatching(/\/methods\/a%2Fb$/),
      expect.anything()
    );
  });
});
