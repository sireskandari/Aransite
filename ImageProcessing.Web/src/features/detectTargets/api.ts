import api from "../../lib/axios";

export type DetectTarget = {
  id: string;
  cameraKey: string;
  targets: string;
};

export async function listDetectTargets(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  const { data, headers } = await api.get("/DetectTargets", { params });
  return {
    items: data.result as DetectTarget[],
    pagination: headers["x-pagination"]
      ? JSON.parse(headers["x-pagination"])
      : null,
  };
}

export async function getDetectTarget(id: string) {
  const { data } = await api.get(`/DetectTargets/${id}`);
  return data.result as DetectTarget;
}

export async function updateDetectTarget(
  id: string,
  payload: { cameraKey: string; targets: string }
) {
  const { data } = await api.put(
    `/DetectTargets/${encodeURIComponent(id)}`,
    payload
  );
  // if API returns wrapper, keep parity:
  return (data?.result ?? data) as DetectTarget;
}

/** DELETE /DetectTargets/{id} */
export async function deleteDetectTarget(id: string) {
  await api.delete(`/DetectTargets/${encodeURIComponent(id)}`);
}

export async function createDetectTarget(payload: {
  cameraKey: string;
  targets: string;
}) {
  const { data } = await api.post("/DetectTargets", payload);
  return data?.result ?? data;
}
