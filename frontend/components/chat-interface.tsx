"use client";

import * as React from "react";
import { ChatMessage, ChatMessageSkeleton, type Message } from "./chat-message";
import { ChatInput } from "./chat-input";
import { ExampleQueries } from "./example-queries";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { askQuestion, ApiError } from "@/lib/api";
import { AlertCircle, RefreshCw } from "lucide-react";

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

    // Add user message
    const userMessage: Message = {
      id: `user-${Date.now()}`,
      role: "user",
      content: question,
    };
    setMessages((prev) => [...prev, userMessage]);
    setIsLoading(true);

    try {
      const response = await askQuestion(question);

      // Add assistant message
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
      // Remove the last user message and try again
      setMessages((prev) => prev.filter((m) => m.id !== lastUserMessage.id));
      handleSubmit(lastUserMessage.content);
    }
  };

  const hasMessages = messages.length > 0;

  return (
    <div className="flex h-full flex-col">
      {/* Messages Area */}
      <div className="flex-1 overflow-y-auto p-4">
        <div className="mx-auto max-w-3xl space-y-6">
          {!hasMessages && !isLoading && (
            <div className="flex flex-col items-center justify-center py-12">
              <h2 className="mb-2 text-2xl font-semibold">
                Ask about your documents
              </h2>
              <p className="text-muted-foreground mb-8 text-center">
                Get AI-powered answers from your pre-processed PDF documents.
              </p>
              <ExampleQueries onSelect={handleSubmit} disabled={isLoading} />
            </div>
          )}

          {messages.map((message) => (
            <ChatMessage key={message.id} message={message} />
          ))}

          {isLoading && <ChatMessageSkeleton />}

          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertTitle>Error</AlertTitle>
              <AlertDescription className="flex items-center justify-between">
                <span>{error}</span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRetry}
                  className="ml-4 shrink-0"
                >
                  <RefreshCw className="mr-2 h-4 w-4" />
                  Retry
                </Button>
              </AlertDescription>
            </Alert>
          )}

          <div ref={messagesEndRef} />
        </div>
      </div>

      {/* Input Area */}
      <div className="bg-background border-t p-4">
        <div className="mx-auto max-w-3xl">
          <ChatInput onSubmit={handleSubmit} isLoading={isLoading} />
          <p className="text-muted-foreground mt-2 text-center text-xs">
            Press Enter to send, Shift+Enter for new line
          </p>
        </div>
      </div>
    </div>
  );
}
