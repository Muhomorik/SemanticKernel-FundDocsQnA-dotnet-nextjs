"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { ChevronDown } from "lucide-react";

const QUERY_GROUPS = [
  {
    title: "Quick Start",
    categories: [
      {
        title: "Getting Started",
        queries: [
          "Show me all available funds",
          "Which fund is best for a beginner?",
          "I want to invest in Swedish stocks",
          "I want low risk",
          "Compare costs of all funds",
          "Which funds pay dividends?",
          "I have a 5-year investment horizon",
          "Show me emerging market options",
        ],
      },
    ],
  },
  {
    title: "Comparison Questions",
    categories: [
      {
        title: "Direct Comparisons",
        queries: [
          "Compare SEB Sverigefond and SEB Sverige Expanderad",
          "Compare the two Emerging Markets funds",
          "Compare all Swedish equity funds",
          "Compare all bond funds",
          "Compare all global/world funds",
          "What's the difference between SEB Nordenfond and SEB Sverigefond?",
        ],
      },
      {
        title: "Best/Worst Analysis",
        queries: [
          "Which fund has the best positive scenario return?",
          "Which fund has the worst stress scenario?",
          "Which fund has the highest neutral scenario return?",
          "Which equity fund has the lowest costs?",
          "Which bond fund has the lowest risk?",
        ],
      },
      {
        title: "Thematic",
        queries: [
          "Which funds are suitable for a conservative investor?",
          "Which funds are suitable for an aggressive investor?",
          "Which funds provide geographic diversification?",
          "Which funds focus on defense and security?",
          "How can I build a diversified portfolio with these funds?",
        ],
      },
    ],
  },
  {
    title: "Specific Funds",
    categories: [
      {
        title: "SEB Korträntefond SEK",
        queries: [
          "Why does this fund have the lowest risk level?",
          "What types of bonds does this fund invest in?",
          "What is the maximum interest rate duration?",
        ],
      },
      {
        title: "SEB European Defence & Security",
        queries: [
          "What sectors does this fund invest in?",
          "Can the fund invest outside Europe?",
          "What is the fund's approach to defense investments?",
        ],
      },
      {
        title: "SEB Asienfond ex Japan",
        queries: [
          "Why is Japan excluded from this fund?",
          "Can the fund invest in mainland China?",
          "What is quantitative analysis in this context?",
        ],
      },
      {
        title: "SEB Världenfond",
        queries: [
          "What is the allocation between stocks and bonds?",
          "How is the Swedish portion managed differently from the global portion?",
          "What does the composite benchmark represent?",
        ],
      },
      {
        title: "SEB Global High Yield",
        queries: [
          "What does 'high yield' mean?",
          "What is the difference between investment grade and non-investment grade bonds?",
          "Why is the energy sector excluded from the benchmark?",
        ],
      },
      {
        title: "SEB USA Indexnära",
        queries: [
          'What does "indexnära" (index-tracking) mean?',
          "Why might the fund deviate from its benchmark?",
          "What is the fund domiciled in Luxembourg?",
        ],
      },
    ],
  },
  {
    title: "Single Fund Questions",
    categories: [
      {
        title: "About the Fund",
        queries: [
          "What is this fund and what does it invest in?",
          "What is the fund's investment objective?",
          "What type of fund is this (securities fund, special fund, UCITS)?",
          "What is the benchmark index?",
          'What does "Article 8" classification mean?',
          "Is this fund actively or passively managed?",
          "What investment strategy does the fund use?",
        ],
      },
      {
        title: "Risk & Returns",
        queries: [
          "What is the fund's risk level (1-7)?",
          "What does the risk indicator mean?",
          "What are the potential returns in different scenarios?",
          "What could I get back in a stress scenario?",
          "What could I get back in a negative scenario?",
          "What could I get back in a neutral scenario?",
          "What could I get back in a positive scenario?",
          "Is there any capital guarantee?",
          "What currency risk should I be aware of?",
          "Is there any protection against market risk?",
        ],
      },
      {
        title: "Costs",
        queries: [
          "What are the total annual costs?",
          "What is the management fee?",
          "What are the transaction costs?",
          "Are there any entry fees?",
          "Are there any exit fees?",
          "Are there any performance fees?",
          "How do costs affect my returns over time?",
          "What is the annual cost impact percentage?",
        ],
      },
      {
        title: "Investing",
        queries: [
          "How long should I hold this investment?",
          "What is the recommended holding period?",
          "Can I sell my shares at any time?",
          "When can I buy and sell shares?",
          "Who is this fund suitable for?",
          "Do I need special knowledge to invest?",
          "What happens if I redeem early?",
        ],
      },
      {
        title: "Practical Info",
        queries: [
          "What is the fund's ISIN code?",
          "Who is the fund manager?",
          "Who is the custodian/depositary?",
          "How do I file a complaint?",
          "Where can I find more information?",
          "What happens if SEB Funds AB goes bankrupt?",
          "Does this share class pay dividends?",
          "Where is the fund domiciled?",
        ],
      },
    ],
  },
];

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
