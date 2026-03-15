import { TextDecoder, TextEncoder } from "util";

// Polyfill for jsdom environment
Object.assign(global, { TextDecoder, TextEncoder });

import { askQuestionStream, ApiError } from "@/lib/api";
import type { StreamCallbacks } from "@/lib/api";

// Mock a ReadableStream reader from string chunks (using Buffer since jsdom lacks TextEncoder)
function createMockReader(chunks: string[]) {
  let index = 0;

  return {
    read: jest.fn().mockImplementation(() => {
      if (index < chunks.length) {
        const value = new Uint8Array(Buffer.from(chunks[index]));
        index++;
        return Promise.resolve({ done: false, value });
      }
      return Promise.resolve({ done: true, value: undefined });
    }),
    releaseLock: jest.fn(),
  };
}

function mockFetchWithSse(chunks: string[], status = 200) {
  const reader = createMockReader(chunks);

  global.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? "OK" : "Bad Request",
    body: { getReader: () => reader },
    text: () => Promise.resolve("error details"),
  });
}

function createCallbacks(): StreamCallbacks & {
  sources: unknown[];
  deltas: string[];
  doneCount: number;
  errors: string[];
} {
  const tracker = {
    sources: [] as unknown[],
    deltas: [] as string[],
    doneCount: 0,
    errors: [] as string[],
    onSources: (s: unknown) => {
      tracker.sources.push(s);
    },
    onDelta: (text: string) => {
      tracker.deltas.push(text);
    },
    onDone: () => {
      tracker.doneCount++;
    },
    onError: (msg: string) => {
      tracker.errors.push(msg);
    },
  };
  return tracker;
}

describe("askQuestionStream", () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  it("parses sources, deltas, and done events in order", async () => {
    const sseData = [
      'event: sources\ndata: [{"file":"doc.pdf","page":1}]\n\n',
      'event: delta\ndata: "Hello "\n\n',
      'event: delta\ndata: "world"\n\n',
      "event: done\ndata: {}\n\n",
    ];
    mockFetchWithSse(sseData);
    const cb = createCallbacks();

    await askQuestionStream("test question", cb);

    expect(cb.sources).toEqual([[{ file: "doc.pdf", page: 1 }]]);
    expect(cb.deltas).toEqual(["Hello ", "world"]);
    expect(cb.doneCount).toBe(1);
    expect(cb.errors).toEqual([]);
  });

  it("handles SSE events split across chunks", async () => {
    // Split an event across two read() calls
    const sseData = [
      "event: sources\ndata: []\n\nevent: del",
      'ta\ndata: "split"\n\nevent: done\ndata: {}\n\n',
    ];
    mockFetchWithSse(sseData);
    const cb = createCallbacks();

    await askQuestionStream("test", cb);

    expect(cb.sources).toEqual([[]]);
    expect(cb.deltas).toEqual(["split"]);
    expect(cb.doneCount).toBe(1);
  });

  it("handles error event from server", async () => {
    const sseData = [
      "event: sources\ndata: []\n\n",
      'event: error\ndata: {"message":"Something went wrong"}\n\n',
    ];
    mockFetchWithSse(sseData);
    const cb = createCallbacks();

    await askQuestionStream("test", cb);

    expect(cb.errors).toEqual(["Something went wrong"]);
    expect(cb.doneCount).toBe(0);
  });

  it("throws ApiError on non-200 response", async () => {
    mockFetchWithSse([], 400);

    await expect(askQuestionStream("test", createCallbacks())).rejects.toThrow(
      ApiError
    );
  });

  it("throws ApiError on empty question", async () => {
    await expect(askQuestionStream("", createCallbacks())).rejects.toThrow(
      ApiError
    );

    await expect(askQuestionStream("   ", createCallbacks())).rejects.toThrow(
      ApiError
    );
  });

  it("sends POST request with correct body and headers", async () => {
    mockFetchWithSse(["event: done\ndata: {}\n\n"]);
    const cb = createCallbacks();

    await askQuestionStream("  my question  ", cb);

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/ask/stream"),
      expect.objectContaining({
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ question: "my question" }),
      })
    );
  });

  it("passes abort signal to fetch", async () => {
    mockFetchWithSse(["event: done\ndata: {}\n\n"]);
    const controller = new AbortController();
    const cb = createCallbacks();

    await askQuestionStream("test", cb, controller.signal);

    expect(global.fetch).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ signal: controller.signal })
    );
  });
});
