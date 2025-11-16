import { useQuery } from "@tanstack/react-query";
import { listCameras, getCamera } from "./api";

export function useCamerasList(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["Cameras", params],
    queryFn: () => listCameras(params),
  });
}

export function useCamera(id: string) {
  return useQuery({
    queryKey: ["Camera", id],
    queryFn: () => getCamera(id),
    enabled: !!id,
  });
}
