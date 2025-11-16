import api from "../../lib/axios";

export type Camera = {
  id: string;
  key: string;
  location: string;
  rtsp: string;
};

export async function listCameras(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  const { data, headers } = await api.get("/Cameras", { params });
  return {
    items: data.result as Camera[],
    pagination: headers["x-pagination"]
      ? JSON.parse(headers["x-pagination"])
      : null,
  };
}

export async function getCamera(id: string) {
  const { data } = await api.get(`/Cameras/${id}`);
  return data.result as Camera;
}

export async function updateCamera(
  id: string,
  payload: { key: string; location: string; rtsp: string; isActive: boolean }
) {
  const { data } = await api.put(`/Cameras/${encodeURIComponent(id)}`, payload);
  // if API returns wrapper, keep parity:
  return (data?.result ?? data) as Camera;
}

/** DELETE /Cameras/{id} */
export async function deleteCamera(id: string) {
  await api.delete(`/Cameras/${encodeURIComponent(id)}`);
}

export async function createCamera(payload: {
  key: string;
  location: string;
  rtsp: string;
  isActive: boolean;
}) {
  const { data } = await api.post("/Cameras", payload);
  return data?.result ?? data;
}
