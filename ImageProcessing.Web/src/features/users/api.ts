import api from "../../lib/axios";

export type User = {
  id: string;
  email: string;
  name: string;
  role: string;
  profileImagePath?: string;
};

export async function listUsers(params: {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}) {
  const { data, headers } = await api.get("/users", { params });
  return {
    items: data.result as User[],
    pagination: headers["x-pagination"]
      ? JSON.parse(headers["x-pagination"])
      : null,
  };
}

export async function getUser(id: string) {
  const { data } = await api.get(`/users/${id}`);
  return data.result as User;
}

export async function uploadUserImage(id: string, file: File) {
  const form = new FormData();
  form.append("file", file);
  const { data } = await api.post(`/users/${id}/upload`, form, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data.result as { id: string; imageUrl: string };
}
