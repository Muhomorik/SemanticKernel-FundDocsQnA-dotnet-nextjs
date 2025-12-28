import { render, screen } from "@testing-library/react";
import Home from "@/app/page";

describe("Home Page", () => {
  it("renders the main heading", () => {
    render(<Home />);
    const heading = screen.getByRole("heading", {
      name: /Ask about your documents/i,
    });
    expect(heading).toBeInTheDocument();
  });

  it("displays the description text", () => {
    render(<Home />);
    const description = screen.getByText(
      /Get AI-powered answers from your pre-processed PDF documents/i
    );
    expect(description).toBeInTheDocument();
  });

  it("shows example query categories and hint", () => {
    render(<Home />);
    // Check for category buttons
    const categories = [
      "Fund Basics",
      "Risk & Returns",
      "Costs",
      "Investing",
      "Practical",
      "Comparison",
    ];
    categories.forEach((cat) => {
      expect(screen.getByRole("button", { name: cat })).toBeInTheDocument();
    });
    // Check for the hint text
    expect(
      screen.getByText(
        /Click a category to see more questions, or click any question to ask it/i
      )
    ).toBeInTheDocument();
  });

  it("renders example query buttons", () => {
    render(<Home />);
    const fundButton = screen.getByRole("button", {
      name: /What is this fund and what does it invest in/i,
    });
    expect(fundButton).toBeInTheDocument();
  });
});
