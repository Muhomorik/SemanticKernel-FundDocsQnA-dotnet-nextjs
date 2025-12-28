# Frontend - PDF Q&A Application

> Part of [PDF Q&A Application](../README.md). See [Configuration & Secrets Guide](../docs/SECRETS-MANAGEMENT.md) for all environment variables.

Next.js frontend for asking questions about PDF documents.

## Tech Stack

- **Next.js 16** - React framework with App Router
- **TypeScript** - Type-safe JavaScript
- **Tailwind CSS** - Utility-first CSS framework
- **shadcn/ui** - Re-usable component library
- **Prettier** - Code formatter
- **EditorConfig** - Consistent editor configuration

## Quick Start

```bash
# Install dependencies
npm install

# Configure environment (copy .env.example to .env.local)
cp .env.example .env.local

# Run development server
npm run dev
```

Visit [http://localhost:3000](http://localhost:3000)

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm start` - Start production server
- `npm run lint` - Run ESLint
- `npm run format` - Auto-format with Prettier
- `npm run format:check` - Check formatting

## Testing

This project uses Jest and React Testing Library for testing.

### Test Frameworks

- **Jest** - JavaScript testing framework
- **React Testing Library** - React component testing utilities
- **@testing-library/jest-dom** - Custom Jest matchers for DOM assertions

### Running Tests

```bash
# Run all tests
npm test

# Run tests in watch mode (re-runs on file changes)
npm run test:watch

# Run tests with coverage report
npm run test:coverage
```

### Writing Tests

Tests are located in the `__tests__` directory, mirroring the app structure:

```
__tests__/
├── app/
│   └── page.test.tsx
└── lib/
    └── api.test.tsx (future)
```

Example test:

```typescript
import { render, screen } from '@testing-library/react'
import Home from '@/app/page'

describe('Home Page', () => {
  it('renders the heading', () => {
    render(<Home />)
    expect(screen.getByRole('heading')).toBeInTheDocument()
  })
})
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `NEXT_PUBLIC_API_URL` | Backend API endpoint URL | `http://localhost:5000` |

Configure in `.env.local`:

```bash
cp .env.example .env.local
# Edit .env.local to set NEXT_PUBLIC_API_URL if needed
```

**Note:** Variables prefixed with `NEXT_PUBLIC_` are exposed to the browser. Never put secrets in these variables.

See **[Configuration & Secrets Guide](../docs/SECRETS-MANAGEMENT.md)** for complete configuration reference.

## Backend API

Backend must be running at `http://localhost:5000` (see [Backend README](../backend/README.md))
