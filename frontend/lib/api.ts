/**
 * API service for communicating with the backend
 */

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export interface AskRequest {
  question: string;
}

export interface SourceReference {
  file: string;
  page: number;
}

export interface AskResponse {
  answer: string;
  sources: SourceReference[];
}

export class ApiError extends Error {
  constructor(
    message: string,
    public statusCode?: number,
    public details?: unknown
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * Ask a question to the backend API
 */
export async function askQuestion(question: string): Promise<AskResponse> {
  if (!question || question.trim().length === 0) {
    throw new ApiError("Question cannot be empty");
  }

  try {
    const response = await fetch(`${API_URL}/api/ask`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ question: question.trim() } as AskRequest),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new ApiError(
        `API request failed: ${response.statusText}`,
        response.status,
        errorText
      );
    }

    const data: AskResponse = await response.json();
    return data;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }

    // Network or other errors
    throw new ApiError(
      error instanceof Error ? error.message : "An unknown error occurred",
      undefined,
      error
    );
  }
}

export interface StreamCallbacks {
  onSources: (sources: SourceReference[]) => void;
  onDelta: (text: string) => void;
  onDone: () => void;
  onError: (error: string) => void;
}

/**
 * Ask a question with streaming response via Server-Sent Events.
 * Tokens arrive incrementally via onDelta; sources arrive first via onSources.
 */
export async function askQuestionStream(
  question: string,
  callbacks: StreamCallbacks,
  signal?: AbortSignal
): Promise<void> {
  if (!question || question.trim().length === 0) {
    throw new ApiError("Question cannot be empty");
  }

  const response = await fetch(`${API_URL}/api/ask/stream`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ question: question.trim() } as AskRequest),
    signal,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new ApiError(
      `API request failed: ${response.statusText}`,
      response.status,
      errorText
    );
  }

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      // Parse complete SSE events (separated by double newline)
      const parts = buffer.split("\n\n");
      buffer = parts.pop()!; // Keep incomplete event in buffer

      for (const part of parts) {
        if (!part.trim()) continue;

        const lines = part.split("\n");
        let eventType = "";
        let data = "";

        for (const line of lines) {
          if (line.startsWith("event: ")) eventType = line.slice(7);
          else if (line.startsWith("data: ")) data = line.slice(6);
        }

        switch (eventType) {
          case "sources":
            callbacks.onSources(JSON.parse(data));
            break;
          case "delta":
            callbacks.onDelta(JSON.parse(data));
            break;
          case "done":
            callbacks.onDone();
            break;
          case "error":
            callbacks.onError(JSON.parse(data).message);
            break;
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}

/**
 * Check if the backend API is healthy
 */
export async function checkHealth(): Promise<boolean> {
  try {
    const response = await fetch(`${API_URL}/health/live`, {
      method: "GET",
    });
    return response.ok;
  } catch {
    return false;
  }
}
