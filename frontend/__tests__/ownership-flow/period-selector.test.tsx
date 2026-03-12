import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { PeriodSelector } from "@/components/ownership-flow/period-selector";
import { OwnershipPeriodsResponse, SelectedPeriod } from "@/lib/ownership-flow";

const PERIODS: OwnershipPeriodsResponse = {
  weekly: [
    { label: "Feb 10 – 16", from: "2025-02-10", to: "2025-02-16" },
    { label: "Feb 17 – 23", from: "2025-02-17", to: "2025-02-23" },
    { label: "Feb 24 – Mar 2", from: "2025-02-24", to: "2025-03-02" },
    { label: "Mar 3 – 10", from: "2025-03-03", to: "2025-03-10" },
  ],
  monthly: [
    { label: "1 month", from: "2025-01-10", to: "2025-02-10" },
    { label: "2 months", from: "2024-12-10", to: "2025-02-10" },
    { label: "3 months", from: "2024-11-10", to: "2025-02-10" },
  ],
};

const DEFAULT_SELECTED: SelectedPeriod = {
  group: "weekly",
  index: 0,
  period: PERIODS.weekly[0],
};

describe("PeriodSelector", () => {
  it("renders all 4 weekly pills", () => {
    render(
      <PeriodSelector periods={PERIODS} selected={DEFAULT_SELECTED} onChange={() => {}} />,
    );
    expect(screen.getByText("Feb 10 – 16")).toBeInTheDocument();
    expect(screen.getByText("Feb 17 – 23")).toBeInTheDocument();
    expect(screen.getByText("Feb 24 – Mar 2")).toBeInTheDocument();
    expect(screen.getByText("Mar 3 – 10")).toBeInTheDocument();
  });

  it("renders all 3 monthly pills", () => {
    render(
      <PeriodSelector periods={PERIODS} selected={DEFAULT_SELECTED} onChange={() => {}} />,
    );
    expect(screen.getByText("1 month")).toBeInTheDocument();
    expect(screen.getByText("2 months")).toBeInTheDocument();
    expect(screen.getByText("3 months")).toBeInTheDocument();
  });

  it("selected weekly pill has aria-pressed=true", () => {
    render(
      <PeriodSelector periods={PERIODS} selected={DEFAULT_SELECTED} onChange={() => {}} />,
    );
    const activeBtn = screen.getByRole("button", { name: "Feb 10 – 16" });
    expect(activeBtn).toHaveAttribute("aria-pressed", "true");
  });

  it("unselected pills have aria-pressed=false", () => {
    render(
      <PeriodSelector periods={PERIODS} selected={DEFAULT_SELECTED} onChange={() => {}} />,
    );
    const inactiveBtn = screen.getByRole("button", { name: "Feb 17 – 23" });
    expect(inactiveBtn).toHaveAttribute("aria-pressed", "false");
  });

  it("calls onChange with correct SelectedPeriod when weekly pill clicked", async () => {
    const onChange = jest.fn();
    render(
      <PeriodSelector periods={PERIODS} selected={DEFAULT_SELECTED} onChange={onChange} />,
    );
    await userEvent.click(screen.getByRole("button", { name: "Feb 17 – 23" }));
    expect(onChange).toHaveBeenCalledWith({
      group: "weekly",
      index: 1,
      period: PERIODS.weekly[1],
    });
  });

  it("calls onChange with group=monthly when monthly pill clicked", async () => {
    const onChange = jest.fn();
    render(
      <PeriodSelector periods={PERIODS} selected={DEFAULT_SELECTED} onChange={onChange} />,
    );
    await userEvent.click(screen.getByRole("button", { name: "1 month" }));
    expect(onChange).toHaveBeenCalledWith({
      group: "monthly",
      index: 0,
      period: PERIODS.monthly[0],
    });
  });

  it("all buttons are disabled when disabled prop is true", () => {
    render(
      <PeriodSelector
        periods={PERIODS}
        selected={DEFAULT_SELECTED}
        onChange={() => {}}
        disabled
      />,
    );
    const buttons = screen.getAllByRole("button");
    buttons.forEach((btn) => expect(btn).toBeDisabled());
  });

  it("selected monthly pill has aria-pressed=true", () => {
    const monthlySelected: SelectedPeriod = {
      group: "monthly",
      index: 1,
      period: PERIODS.monthly[1],
    };
    render(
      <PeriodSelector periods={PERIODS} selected={monthlySelected} onChange={() => {}} />,
    );
    expect(screen.getByRole("button", { name: "2 months" })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(screen.getByRole("button", { name: "1 month" })).toHaveAttribute(
      "aria-pressed",
      "false",
    );
  });
});
