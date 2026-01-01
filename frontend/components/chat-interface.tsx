"use client";

import * as React from "react";
import { ChatMessage, ChatMessageSkeleton, type Message } from "./chat-message";
import { ChatInput } from "./chat-input";
import { ExampleQueries } from "./example-queries";
import { Button } from "@/components/ui/button";
import { askQuestion, ApiError } from "@/lib/api";
import { AlertCircle, RotateCcw } from "lucide-react";

export function ChatInterface() {
  const [messages, setMessages] = React.useState<Message[]>([]);
  const [isLoading, setIsLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const messagesEndRef = React.useRef<HTMLDivElement>(null);

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

    try {
      const response = await askQuestion(question);

      const assistantMessage: Message = {
        id: `assistant-${Date.now()}`,
        role: "assistant",
        content: response.answer,
        sources: response.sources,
      };
      setMessages((prev) => [...prev, assistantMessage]);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.statusCode === 400) {
          setError("Please enter a valid question (at least 3 characters).");
        } else if (err.statusCode && err.statusCode >= 500) {
          setError("The server encountered an error. Please try again later.");
        } else if (!err.statusCode) {
          setError(
            "Unable to connect to the server. Please check your connection."
          );
        } else {
          setError(err.message);
        }
      } else {
        setError("An unexpected error occurred. Please try again.");
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
      <div className="border-b border-border/30 bg-muted/20 px-4 py-2">
        <div className="mx-auto max-w-3xl">
          <p className="text-muted-foreground text-center text-xs leading-relaxed">
            <span className="font-medium">Demo Notice:</span> This site runs on free tier resources and may experience downtime when limits are reached. Currently processing 15 of 68 SEB funds.
          </p>
        </div>
      </div>

      {/* Hero section - only shown when no messages */}
      {!hasMessages && !isLoading && (
        <div className="flex flex-col items-center px-6 pt-6 pb-4">
          <div className="animate-fade-up text-center">
            <h1 className="font-serif text-2xl font-medium tracking-tight sm:text-3xl">
              Ask about your documents
            </h1>
            <p className="text-muted-foreground mt-2 max-w-md text-sm">
              Get AI-powered answers from your pre-processed PDF documents with
              source references.
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

              {isLoading && <ChatMessageSkeleton />}

              {error && (
                <div className="animate-fade-up bg-destructive/5 border-destructive/20 flex items-start gap-3 rounded-xl border p-4">
                  <AlertCircle className="text-destructive mt-0.5 h-5 w-5 shrink-0" />
                  <div className="flex-1">
                    <p className="text-destructive text-sm font-medium">
                      Something went wrong
                    </p>
                    <p className="text-destructive/80 mt-1 text-sm">{error}</p>
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
              <ChatInput onSubmit={handleSubmit} isLoading={isLoading} />
            </div>
          </div>
        </>
      )}
    </div>
  );
}
