"use client";

import { useId, useState } from "react";
import { Search } from "lucide-react";
import { Fund, searchFunds } from "@/lib/fund-clusters";

interface FundSearchProps {
  funds: Fund[];
  onPick: (isin: string) => void;
}

export function FundSearch({ funds, onPick }: FundSearchProps) {
  const listId = useId();
  const [query, setQuery] = useState("");
  const [matches, setMatches] = useState<Fund[]>([]);
  const [selected, setSelected] = useState(0);
  const open = matches.length > 0;

  function close() {
    setMatches([]);
  }

  function choose(fund: Fund) {
    setQuery(fund.name);
    close();
    onPick(fund.isin);
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "ArrowDown" || e.key === "ArrowUp") {
      e.preventDefault();
      if (!open) return;
      const step = e.key === "ArrowDown" ? 1 : -1;
      setSelected((selected + step + matches.length) % matches.length);
    } else if (e.key === "Enter" && matches[selected]) {
      e.preventDefault();
      choose(matches[selected]);
    } else if (e.key === "Escape") {
      close();
    }
  }

  return (
    <div className="relative min-w-0 flex-1 basis-60 sm:max-w-80">
      <Search
        aria-hidden="true"
        className="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 h-3.5 w-3.5 -translate-y-1/2"
      />
      <input
        type="search"
        role="combobox"
        aria-label="Find a fund"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={open ? `${listId}-${selected}` : undefined}
        placeholder="Find a fund by name or ISIN"
        autoComplete="off"
        value={query}
        onChange={(e) => {
          setQuery(e.target.value);
          setMatches(searchFunds(funds, e.target.value));
          setSelected(0);
        }}
        onKeyDown={handleKeyDown}
        onBlur={close}
        className="bg-background border-border placeholder:text-muted-foreground/70 focus-visible:ring-ring/40 w-full rounded-md border py-1.5 pr-2 pl-8 text-sm outline-none focus-visible:ring-2"
      />
      {open && (
        <ul
          id={listId}
          role="listbox"
          className="bg-popover border-border absolute inset-x-0 top-[calc(100%+4px)] z-20 rounded-lg border p-1 shadow-lg"
        >
          {matches.map((fund, i) => (
            <li
              key={fund.isin}
              id={`${listId}-${i}`}
              role="option"
              aria-selected={i === selected}
              onMouseDown={(e) => {
                e.preventDefault();
                choose(fund);
              }}
              className="aria-selected:bg-accent hover:bg-accent cursor-pointer rounded-md px-2 py-1.5 text-sm"
            >
              {fund.name}
              <span className="text-muted-foreground block text-xs">
                {fund.isin}, {fund.category}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
