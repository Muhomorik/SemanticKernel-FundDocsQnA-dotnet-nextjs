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
