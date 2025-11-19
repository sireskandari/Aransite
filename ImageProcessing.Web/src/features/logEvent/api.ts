import api from "../../lib/axios";

export type LogEvent = {
  id: number;
  timestamp: string;
  level: string;
  template: string;
  message: string;
  exception: string;
  properties: string;
};

export async function listLogEvents(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  const { data, headers } = await api.get("/LogEvents", { params });
  return {
    items: data.result as LogEvent[],
    pagination: headers["x-pagination"]
      ? JSON.parse(headers["x-pagination"])
      : null,
  };
}

export async function getLogEvent(id: string) {
  const { data } = await api.get(`/LogEvents/${id}`);
  return data.result as LogEvent;
}

/** DELETE /LogEvents/{id} */
export async function deleteLogEvent() {
  await api.delete(`/LogEvents/`);
}
