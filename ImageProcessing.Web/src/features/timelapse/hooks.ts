import { useQuery } from "@tanstack/react-query";
import { listTimelapses, getTimelapse } from "./api";

export function useTimelapsesList(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["Timelapses", params],
    queryFn: () => listTimelapses(params),
  });
}

export function useTimelapse(id: string) {
  return useQuery({
    queryKey: ["Timelapse", id],
    queryFn: () => getTimelapse(id),
    enabled: !!id,
  });
}
