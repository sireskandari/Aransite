import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listUsers, getUser, uploadUserImage } from "./api";

export function useUsersList(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["users", params],
    queryFn: () => listUsers(params),
  });
}

export function useUser(id: string) {
  return useQuery({
    queryKey: ["user", id],
    queryFn: () => getUser(id),
    enabled: !!id,
  });
}

export function useUploadUserImage(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => uploadUserImage(id, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["users"] });
      qc.invalidateQueries({ queryKey: ["user", id] });
    },
  });
}
