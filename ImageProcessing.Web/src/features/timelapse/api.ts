import api from "../../lib/axios";

export type Timelapse = {
  id: string;
  filePath: string;
  fileFormat: string;
  fileSize: string;
  status: string;
  errorMessage: string;
};

export async function listTimelapses(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  const { data, headers } = await api.get("/Timelapse", { params });
  return {
    items: data.result as Timelapse[],
    pagination: headers["x-pagination"]
      ? JSON.parse(headers["x-pagination"])
      : null,
  };
}

export async function getTimelapse(id: string) {
  const { data } = await api.get(`/Timelapse/${id}`);
  return data.result as Timelapse;
}

export async function updateTimelapse(
  id: string,
  payload: {
    filePath: string;
    fileFormat: string;
    fileSize: string;
    status: string;
    errorMessage: string;
  }
) {
  const { data } = await api.put(
    `/Timelapse/${encodeURIComponent(id)}`,
    payload
  );
  // if API returns wrapper, keep parity:
  return (data?.result ?? data) as Timelapse;
}

/** DELETE /Timelapse/{id} */
export async function deleteTimelapse(id: string) {
  await api.delete(`/Timelapse/${encodeURIComponent(id)}`);
}

export async function createTimelapse(payload: {
  filePath: string;
  fileFormat: string;
  fileSize: string;
  status: string;
  errorMessage: string;
}) {
  const { data } = await api.post("/Timelapse", payload);
  return data?.result ?? data;
}
