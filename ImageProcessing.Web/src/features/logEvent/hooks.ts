import { useQuery } from "@tanstack/react-query";
import { listLogEvents, getLogEvent } from "./api";

export function useLogEventsList(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["LogEvents", params],
    queryFn: () => listLogEvents(params),
  });
}

export function useLogEvent(id: string) {
  return useQuery({
    queryKey: ["LogEvent", id],
    queryFn: () => getLogEvent(id),
    enabled: !!id,
  });
}
