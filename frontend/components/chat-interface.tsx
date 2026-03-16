"use client";

import * as React from "react";
import { ChatMessage, ChatMessageSkeleton, type Message } from "./chat-message";
import { ChatInput } from "./chat-input";
import { ExampleQueries } from "./example-queries";
import { Button } from "@/components/ui/button";
import { askQuestionStream, ApiError } from "@/lib/api";
import { AlertCircle, RotateCcw, Plus, Snowflake } from "lucide-react";
import { useChatContext } from "./chat-context";
import { DemoBanner } from "./demo-banner";

export function ChatInterface() {
  const [messages, setMessages] = React.useState<Message[]>([]);
  const [isLoading, setIsLoading] = React.useState(false);
  // Error variant: "cold-start" for Azure App Service cold start errors (502, 503, network timeout),
  // "destructive" for all other errors (400, 500, unexpected)
  const [error, setError] = React.useState<{
    message: string;
    variant: "destructive" | "cold-start";
  } | null>(null);
  const messagesEndRef = React.useRef<HTMLDivElement>(null);
  const { shouldReset, clearReset, resetChat } = useChatContext();

  React.useEffect(() => {
    if (shouldReset) {
      setMessages([]);
      setError(null);
      setIsLoading(false);
      clearReset();
    }
  }, [shouldReset, clearReset]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  React.useEffect(() => {
    scrollToBottom();
  }, [messages, isLoading]);

  const handleSubmit = async (question: string) => {
    setError(null);

    const userMessage: Message = {
      id: `user-${Date.now()}`,
      role: "user",
      content: question,
    };
    setMessages((prev) => [...prev, userMessage]);
    setIsLoading(true);

    const assistantId = `assistant-${Date.now()}`;
    const abortController = new AbortController();

    try {
      await askQuestionStream(
        question,
        {
          onSources: (sources) => {
            // Create assistant message with sources, content starts empty
            const msg: Message = {
              id: assistantId,
              role: "assistant",
              content: "",
              sources,
              isStreaming: true,
            };
            setMessages((prev) => [...prev, msg]);
          },
          onDelta: (text) => {
            setMessages((prev) =>
              prev.map((m) =>
                m.id === assistantId ? { ...m, content: m.content + text } : m
              )
            );
          },
          onDone: () => {
            setMessages((prev) =>
              prev.map((m) =>
                m.id === assistantId ? { ...m, isStreaming: false } : m
              )
            );
          },
          onError: (errorMsg) => {
            setMessages((prev) =>
              prev.map((m) =>
                m.id === assistantId ? { ...m, isStreaming: false } : m
              )
            );
            setError({ message: errorMsg, variant: "destructive" });
          },
        },
        abortController.signal
      );
    } catch (err) {
      if (err instanceof ApiError) {
        // Azure App Service cold start: IIS reverse proxy returns 502 (Bad Gateway)
        // when Kestrel hasn't started yet, or 503 (Service Unavailable) when the
        // app pool is recycling. See: https://learn.microsoft.com/azure/app-service/troubleshoot-http-502-http-503
        if (err.statusCode === 502 || err.statusCode === 503) {
          setError({
            message:
              "The server is waking up from a cold start — this usually takes ~30 seconds. Please retry shortly.",
            variant: "cold-start",
          });
        } else if (err.statusCode === 400) {
          setError({
            message: "Please enter a valid question (at least 3 characters).",
            variant: "destructive",
          });
        } else if (err.statusCode && err.statusCode >= 500) {
          setError({
            message: "The server encountered an error. Please try again later.",
            variant: "destructive",
          });
        } else if (!err.statusCode) {
          // No status code means the fetch threw a network error (TypeError: Failed to fetch).
          // On Azure free tier this can happen when the app is completely cold and the
          // connection times out before the reverse proxy responds.
          setError({
            message:
              "Unable to reach the server — it may be waking up from a cold start. Please retry in ~30 seconds.",
            variant: "cold-start",
          });
        } else {
          setError({ message: err.message, variant: "destructive" });
        }
      } else {
        setError({
          message: "An unexpected error occurred. Please try again.",
          variant: "destructive",
        });
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleRetry = () => {
    const lastUserMessage = [...messages]
      .reverse()
      .find((m) => m.role === "user");
    if (lastUserMessage) {
      setMessages((prev) => prev.filter((m) => m.id !== lastUserMessage.id));
      handleSubmit(lastUserMessage.content);
    }
  };

  const hasMessages = messages.length > 0;

  return (
    <div className="flex h-full flex-col">
      {/* Info Banner - always visible */}
      <DemoBanner />

      {/* Hero section - only shown when no messages */}
      {!hasMessages && !isLoading && (
        <div className="flex flex-col items-center px-6 pt-6 pb-4">
          <div className="animate-fade-up text-center">
            <h1 className="font-serif text-2xl font-medium tracking-tight sm:text-3xl">
              Ask anything about your funds
            </h1>
            <p className="text-muted-foreground mt-2 max-w-md text-sm">
              Hybrid AI answers from PDF factsheets and fund data — powered by
              Semantic Kernel, OpenAI, and function calling.
            </p>
          </div>
        </div>
      )}

      {/* Input area - always visible at top when no messages, or at bottom when chatting */}
      {!hasMessages && !isLoading && (
        <div className="mx-auto w-full max-w-2xl px-6 py-3">
          <ChatInput onSubmit={handleSubmit} isLoading={isLoading} />
        </div>
      )}

      {/* Example queries - positioned BELOW input when no messages */}
      {!hasMessages && !isLoading && (
        <div className="mx-auto w-full max-w-3xl px-6 py-4">
          <ExampleQueries onSelect={handleSubmit} disabled={isLoading} />
        </div>
      )}

      {/* Messages area - shown when there are messages */}
      {(hasMessages || isLoading) && (
        <>
          <div className="flex-1 overflow-y-auto px-4 py-6">
            <div className="mx-auto max-w-3xl space-y-6">
              {messages.map((message) => (
                <ChatMessage key={message.id} message={message} />
              ))}

              {isLoading && !messages.some((m) => m.isStreaming) && (
                <ChatMessageSkeleton />
              )}

              {error && error.variant === "cold-start" && (
                // Cold-start alert: friendly blue styling with snowflake icon.
                // Shown for 502/503 (Azure App Service cold start) and network timeouts.
                <div className="animate-fade-up flex items-start gap-3 rounded-xl border border-sky-200 bg-sky-50 p-4 dark:border-sky-800 dark:bg-sky-950/40">
                  <Snowflake className="mt-0.5 h-5 w-5 shrink-0 text-sky-500" />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-sky-700 dark:text-sky-300">
                      Server is warming up
                    </p>
                    <p className="mt-1 text-sm text-sky-600/80 dark:text-sky-400/80">
                      {error.message}
                    </p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleRetry}
                    className="shrink-0 border-sky-300 text-sky-700 hover:bg-sky-100 dark:border-sky-700 dark:text-sky-300 dark:hover:bg-sky-900"
                  >
                    <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
                    Retry
                  </Button>
                </div>
              )}

              {error && error.variant === "destructive" && (
                <div className="animate-fade-up bg-destructive/5 border-destructive/20 flex items-start gap-3 rounded-xl border p-4">
                  <AlertCircle className="text-destructive mt-0.5 h-5 w-5 shrink-0" />
                  <div className="flex-1">
                    <p className="text-destructive text-sm font-medium">
                      Something went wrong
                    </p>
                    <p className="text-destructive/80 mt-1 text-sm">
                      {error.message}
                    </p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleRetry}
                    className="border-destructive/30 text-destructive hover:bg-destructive/10 shrink-0"
                  >
                    <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
                    Retry
                  </Button>
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>
          </div>

          {/* Input area at bottom when chatting */}
          <div className="border-border/40 bg-background/80 border-t backdrop-blur-xl">
            <div className="mx-auto max-w-3xl px-4 py-4">
              <div className="mb-2 flex justify-center">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={resetChat}
                  className="text-muted-foreground hover:text-foreground gap-1.5 text-xs"
                >
                  <Plus className="h-3.5 w-3.5" />
                  New chat
                </Button>
              </div>
              <ChatInput onSubmit={handleSubmit} isLoading={isLoading} />
            </div>
          </div>
        </>
      )}
    </div>
  );
}
