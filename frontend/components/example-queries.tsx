"use client";

import { Button } from "@/components/ui/button";
import { MessageSquare } from "lucide-react";

const EXAMPLE_QUERIES = [
  "What is this fund and what does it invest in?",
  "What is the fund's risk level?",
  "What are the total annual costs?",
  "How long should I hold this investment?",
];

interface ExampleQueriesProps {
  onSelect: (query: string) => void;
  disabled?: boolean;
}

export function ExampleQueries({ onSelect, disabled }: ExampleQueriesProps) {
  return (
    <div className="flex flex-col items-center gap-4">
      <div className="text-muted-foreground flex items-center gap-2">
        <MessageSquare className="h-4 w-4" />
        <span className="text-sm">Try asking:</span>
      </div>
      <div className="flex flex-wrap justify-center gap-2">
        {EXAMPLE_QUERIES.map((query) => (
          <Button
            key={query}
            variant="outline"
            size="sm"
            onClick={() => onSelect(query)}
            disabled={disabled}
            className="h-auto px-3 py-2 text-left text-xs whitespace-normal"
          >
            {query}
          </Button>
        ))}
      </div>
    </div>
  );
}
