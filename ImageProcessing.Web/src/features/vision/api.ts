import api from "../../lib/axios";

export type EdgeData = {
  id: string;
  CaptureTimestampUtc: Date;
  CreatedUtc: Date;
  CameraId: string;
  ComputeModel: string;
  ComputeInferenceMs: string;
  ImageWidth: number;
  ImageHeight: number;
  Detections: string;
  FrameRawUrl: string;
  FrameAnnotatedUrl: string;
};

<<<<<<< HEAD
export async function generateTimelapseFromEdge(payload: {
  search?: string;
  fromUtc?: string;
  toUtc?: string;
  fps?: number;
  width?: number;
  maxFrames?: number;
  crf?: number;
  preset?: string;
}) {
  const { data } = await api.post("/Timelapse/from-edge/stream", {
    search: payload.search ?? null,
    fromUtc: payload.fromUtc ?? null,
    toUtc: payload.toUtc ?? null,
    fps: payload.fps ?? 20,
    width: payload.width ?? 0,
    maxFrames: payload.maxFrames ?? 5000,
    crf: payload.crf ?? 18,
    preset: payload.preset ?? "veryfast",
  });

  return data?.downloadUrl as string;
}

export async function listEdgeData(params: {
  search?: string;
  fromDate?: string;
  toDate?: string;
=======
export async function listEdgeData(params: {
  search?: string;
>>>>>>> b186aa7 (v4)
  pageNumber?: number;
  pageSize?: number;
}) {
  const { data, headers } = await api.get("/EdgeData", { params });
  return {
    items: data.result as EdgeData[],
    pagination: headers["x-pagination"]
      ? JSON.parse(headers["x-pagination"])
      : null,
  };
}

export async function getEdgeData(id: string) {
  const { data } = await api.get(`/EdgeData/${id}`);
  return data.result as EdgeData;
}

export async function updateEdgeData(
  id: string,
  payload: { key: string; location: string; rtsp: string; isActive: boolean }
) {
  const { data } = await api.put(
    `/EdgeData/${encodeURIComponent(id)}`,
    payload
  );
  // if API returns wrapper, keep parity:
  return (data?.result ?? data) as EdgeData;
}

/** DELETE /EdgeData/{id} */
export async function deleteEdgeData(id: string) {
  await api.delete(`/EdgeData/${encodeURIComponent(id)}`);
}

export async function createEdgeData(payload: {
  CaptureTimestampUtc: Date;
  CameraId: string;
  ComputeModel: string;
  ComputeInferenceMs: string;
  ImageWidth: number;
  ImageHeight: number;
  Detections: string;
  FrameRawUrl: string;
  FrameAnnotatedUrl: string;
}) {
  const { data } = await api.post("/EdgeData", payload);
  return data?.result ?? data;
}
