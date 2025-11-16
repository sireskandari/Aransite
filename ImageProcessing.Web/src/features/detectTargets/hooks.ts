import { useQuery } from "@tanstack/react-query";
import { listDetectTargets, getDetectTarget } from "./api";

export function useDetectTargetsList(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["DetectTargets", params],
    queryFn: () => listDetectTargets(params),
  });
}

export function useDetectTarget(id: string) {
  return useQuery({
    queryKey: ["DetectTarget", id],
    queryFn: () => getDetectTarget(id),
    enabled: !!id,
  });
}
