"use client";

import * as React from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { ArrowUp, Loader2 } from "lucide-react";

interface ChatInputProps {
  onSubmit: (question: string) => void;
  isLoading: boolean;
  disabled?: boolean;
}

export function ChatInput({ onSubmit, isLoading, disabled }: ChatInputProps) {
  const [value, setValue] = React.useState("");
  const textareaRef = React.useRef<HTMLTextAreaElement>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (value.trim() && !isLoading && !disabled) {
      onSubmit(value.trim());
      setValue("");
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  const canSubmit = value.trim() && !isLoading && !disabled;

  return (
    <form onSubmit={handleSubmit} className="relative">
      <div className="bg-card border-border/60 focus-within:border-border focus-within:ring-ring/20 relative overflow-hidden rounded-2xl border shadow-sm transition-all duration-200 focus-within:ring-2">
        <Textarea
          ref={textareaRef}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Ask a question about your documents..."
          disabled={isLoading || disabled}
          className="placeholder:text-muted-foreground/60 min-h-14 resize-none border-0 bg-transparent px-4 py-4 pr-14 text-base shadow-none focus-visible:ring-0"
          rows={1}
        />
        <div className="absolute right-2 bottom-2">
          <Button
            type="submit"
            disabled={!canSubmit}
            size="icon"
            className="bg-primary hover:bg-primary/90 disabled:bg-muted h-10 w-10 rounded-xl shadow-sm transition-all duration-200 disabled:opacity-50"
          >
            {isLoading ? (
              <Loader2 className="h-4.5 w-4.5 animate-spin" strokeWidth={2} />
            ) : (
              <ArrowUp className="h-4.5 w-4.5" strokeWidth={2} />
            )}
            <span className="sr-only">Send message</span>
          </Button>
        </div>
      </div>
      <p className="text-muted-foreground/60 mt-2 text-center text-xs">
        Press Enter to send · Shift+Enter for new line
      </p>
    </form>
  );
}
