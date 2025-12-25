import { render, screen } from '@testing-library/react'
import Home from '@/app/page'

describe('Home Page', () => {
  it('renders the main heading', () => {
    render(<Home />)
    const heading = screen.getByRole('heading', { name: /PDF Q&A Application/i })
    expect(heading).toBeInTheDocument()
  })

  it('displays the description text', () => {
    render(<Home />)
    const description = screen.getByText(/Ask questions about your pre-processed PDF documents/i)
    expect(description).toBeInTheDocument()
  })

  it('shows coming soon message', () => {
    render(<Home />)
    const message = screen.getByText(/Chat interface coming soon/i)
    expect(message).toBeInTheDocument()
  })
})
