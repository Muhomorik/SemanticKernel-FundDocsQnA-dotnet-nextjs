"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { QUERY_GROUPS } from "@/lib/example-queries-data";
import { ChevronDown } from "lucide-react";

interface ExampleQueriesProps {
  onSelect: (query: string) => void;
  disabled?: boolean;
}

export function ExampleQueries({ onSelect, disabled }: ExampleQueriesProps) {
  const [activeGroup, setActiveGroup] = React.useState<string | null>(
    QUERY_GROUPS[0]?.title ?? null
  );
  const [activeCategory, setActiveCategory] = React.useState<string | null>(
    null
  );

  return (
    <div className="w-full space-y-4">
      {/* Level 1: Group Buttons */}
      {QUERY_GROUPS.map((group, groupIndex) => (
        <div key={group.title} className="space-y-3">
          {/* Group Button */}
          <button
            onClick={() =>
              setActiveGroup(activeGroup === group.title ? null : group.title)
            }
            disabled={disabled}
            className={cn(
              "animate-fade-up rounded-lg px-5 py-3 text-base font-bold transition-all duration-300",
              "flex w-full max-w-2xl items-center justify-between border-2",
              "focus-visible:ring-primary/50 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none",
              activeGroup === group.title
                ? "border-primary from-primary/15 to-primary/10 text-primary shadow-primary/10 bg-gradient-to-r shadow-lg"
                : "border-border/60 bg-card text-foreground hover:border-primary/30 shadow-md hover:shadow-lg",
              disabled && "pointer-events-none opacity-50"
            )}
            style={{ animationDelay: `${groupIndex * 50}ms` }}
          >
            <span>{group.title}</span>
            <ChevronDown
              className={cn(
                "h-5 w-5 transition-transform duration-400 ease-out",
                activeGroup === group.title && "rotate-180"
              )}
              strokeWidth={2.5}
            />
          </button>

          {/* Level 2: Categories (collapse/expand) */}
          <div
            className={cn(
              "grid gap-2 overflow-hidden transition-all duration-400",
              activeGroup === group.title
                ? "grid-rows-[1fr] opacity-100"
                : "grid-rows-[0fr] opacity-0"
            )}
          >
            <div className="overflow-hidden">
              {activeGroup === group.title && (
                <div className="flex flex-wrap justify-center gap-2 pl-4">
                  {/* Category Buttons */}
                  {group.categories.map((category, catIndex) => (
                    <div key={category.title} className="w-full">
                      <button
                        onClick={() =>
                          setActiveCategory(
                            activeCategory === category.title
                              ? null
                              : category.title
                          )
                        }
                        disabled={disabled}
                        className={cn(
                          "animate-scale-in rounded-md px-4 py-2.5 text-sm font-semibold transition-all duration-300",
                          "flex items-center gap-1.5 border-2",
                          "focus-visible:ring-warm/50 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none",
                          activeCategory === category.title
                            ? "border-warm/40 from-warm/10 to-warm/15 text-warm shadow-warm/20 hover:shadow-warm/30 bg-gradient-to-b shadow-[0_2px_8px_-2px] hover:scale-[1.01]"
                            : "border-border from-card to-accent/50 text-foreground hover:border-border/80 bg-gradient-to-b shadow-sm hover:shadow-md",
                          disabled && "pointer-events-none opacity-50"
                        )}
                        style={{ animationDelay: `${catIndex * 30}ms` }}
                      >
                        {category.title}
                        <ChevronDown
                          className={cn(
                            "ml-auto h-4 w-4 transition-transform duration-400 ease-out",
                            activeCategory === category.title && "rotate-180"
                          )}
                          strokeWidth={2.5}
                        />
                      </button>

                      {/* Level 3: Queries (collapse/expand) */}
                      <div
                        className={cn(
                          "mt-2 grid gap-2 overflow-hidden transition-all duration-300",
                          activeCategory === category.title
                            ? "grid-rows-[1fr] opacity-100"
                            : "grid-rows-[0fr] opacity-0"
                        )}
                      >
                        <div className="overflow-hidden">
                          {activeCategory === category.title && (
                            <div className="flex flex-wrap justify-center gap-2">
                              {/* Query Buttons */}
                              {category.queries.map((query, queryIndex) => (
                                <button
                                  key={query}
                                  onClick={() => onSelect(query)}
                                  disabled={disabled}
                                  className={cn(
                                    "animate-scale-in text-left",
                                    "bg-secondary/40 hover:bg-secondary/70 border-border/30 hover:border-border/50 text-foreground/80 hover:text-foreground",
                                    "rounded-xl border px-3 py-2 text-sm transition-all duration-200",
                                    "max-w-xs truncate",
                                    disabled && "pointer-events-none opacity-50"
                                  )}
                                  style={{
                                    animationDelay: `${queryIndex * 30}ms`,
                                  }}
                                >
                                  {query}
                                </button>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      ))}

      {/* Hint text */}
      <p className="text-muted-foreground/50 text-center text-xs">
        Select a topic group, then choose a category to see questions
      </p>
    </div>
  );
}
