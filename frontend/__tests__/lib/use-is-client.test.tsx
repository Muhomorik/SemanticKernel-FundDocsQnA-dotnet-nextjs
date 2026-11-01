import { renderHook } from "@testing-library/react";
import { renderToString } from "react-dom/server";
import { useIsClient } from "@/lib/use-is-client";

function Probe() {
  return <span>{useIsClient() ? "client" : "server"}</span>;
}

describe("useIsClient", () => {
  it("returns true when rendered in the browser", () => {
    const { result } = renderHook(() => useIsClient());

    expect(result.current).toBe(true);
  });

  it("returns false during server rendering", () => {
    const html = renderToString(<Probe />);

    expect(html).toContain("server");
  });
});
