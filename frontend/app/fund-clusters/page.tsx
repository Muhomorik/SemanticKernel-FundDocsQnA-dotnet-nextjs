import type { Metadata } from "next";
import { FundClustersPage } from "@/components/fund-clusters/fund-clusters-page";

export const metadata: Metadata = {
  title: "Fund Clusters — Fund Insights",
  description:
    "Map of equity funds grouped by their country and sector holdings, compared with their categories.",
};

export default function FundClustersRoute() {
  return <FundClustersPage />;
}
