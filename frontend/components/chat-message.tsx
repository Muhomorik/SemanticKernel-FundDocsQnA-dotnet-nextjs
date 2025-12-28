import { cn } from "@/lib/utils";
import { User, Sparkles, FileText } from "lucide-react";
import type { SourceReference } from "@/lib/api";

export interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
  sources?: SourceReference[];
}

interface ChatMessageProps {
  message: Message;
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === "user";

  return (
    <div
      className={cn(
        "animate-fade-up flex gap-3",
        isUser ? "flex-row-reverse" : "flex-row"
      )}
    >
      {/* Avatar */}
      <div
        className={cn(
          "flex h-8 w-8 shrink-0 items-center justify-center rounded-xl transition-colors",
          isUser ? "bg-primary text-primary-foreground" : "bg-warm/10 text-warm"
        )}
      >
        {isUser ? (
          <User className="h-4 w-4" strokeWidth={1.5} />
        ) : (
          <Sparkles className="h-4 w-4" strokeWidth={1.5} />
        )}
      </div>

      {/* Message content */}
      <div
        className={cn(
          "flex max-w-[85%] flex-col gap-2 sm:max-w-[75%]",
          isUser ? "items-end" : "items-start"
        )}
      >
        {/* Message bubble */}
        <div
          className={cn(
            "rounded-2xl px-4 py-3 shadow-sm",
            isUser
              ? "bg-primary text-primary-foreground rounded-br-md"
              : "bg-card border-border/50 text-card-foreground rounded-bl-md border"
          )}
        >
          <p className="text-sm leading-relaxed whitespace-pre-wrap">
            {message.content}
          </p>
        </div>

        {/* Source references */}
        {message.sources && message.sources.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {message.sources.map((source, index) => (
              <span
                key={`${source.file}-${source.page}-${index}`}
                className="bg-muted/50 text-muted-foreground border-border/30 inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs"
              >
                <FileText className="h-3 w-3" strokeWidth={1.5} />
                <span className="max-w-32 truncate">{source.file}</span>
                <span className="text-muted-foreground/60">
                  p.{source.page}
                </span>
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export function ChatMessageSkeleton() {
  return (
    <div className="animate-fade-in flex gap-3">
      {/* Avatar skeleton */}
      <div className="bg-muted h-8 w-8 shrink-0 animate-pulse rounded-xl" />

      {/* Content skeleton */}
      <div className="flex flex-col gap-2">
        <div className="bg-muted h-24 w-64 animate-pulse rounded-2xl rounded-bl-md sm:w-80" />
        <div className="flex gap-1.5">
          <div className="bg-muted h-5 w-20 animate-pulse rounded-full" />
          <div className="bg-muted h-5 w-16 animate-pulse rounded-full" />
        </div>
      </div>
    </div>
  );
}
