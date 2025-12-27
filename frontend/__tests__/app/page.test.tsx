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

  it("shows example queries", () => {
    render(<Home />);
    const exampleText = screen.getByText(/Try asking:/i);
    expect(exampleText).toBeInTheDocument();
  });

  it("renders example query buttons", () => {
    render(<Home />);
    const fundButton = screen.getByRole("button", {
      name: /What is this fund and what does it invest in/i,
    });
    expect(fundButton).toBeInTheDocument();
  });
});
