import type { Metadata } from "next";
import { OwnershipFlowPage } from "@/components/ownership-flow/ownership-flow-page";

export const metadata: Metadata = {
  title: "Ownership Flow — Fund Insights",
  description:
    "Visualize investor movement across funds based on weekly ownership changes.",
};

export default function OwnershipFlowRoute() {
  return <OwnershipFlowPage />;
}
