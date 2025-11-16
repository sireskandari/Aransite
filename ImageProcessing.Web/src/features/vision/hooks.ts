<<<<<<< HEAD
import { useQuery } from "@tanstack/react-query";
=======
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
>>>>>>> b186aa7 (v4)
import { listEdgeData, getEdgeData } from "./api";

export function useEdgeDataList(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["EdgeData", params],
    queryFn: () => listEdgeData(params),
  });
}

export function useEdgeData(id: string) {
  return useQuery({
    queryKey: ["EdgeData", id],
    queryFn: () => getEdgeData(id),
    enabled: !!id,
  });
}
