import { apiClient } from "../../shared/http/client";

/** A kind of resource: a system type (`Equipment`, `Utensil`, `Vehicle`, `Other`) or one of the organization's own. */
export interface ResourceType {
  id: string;
  name: string;
}

export interface RegisterResource {
  name: string;
  typeId: string;
  description?: string;
}

export interface Resource {
  id: string;
  name: string;
  typeId: string;
  typeName: string;
  description: string | null;
  status: "Available";
  organizationId: string;
}

export const resourcesService = {
  /** Lists the resource types the organization can use, by name. */
  async listTypes(): Promise<ResourceType[]> {
    const { data } = await apiClient.get<ResourceType[]>("/resource-types");
    return data;
  },

  /**
   * Registers an available resource in the signed-in account's organization.
   *
   * @throws The Axios error; 400 carries per-field messages (`name`, `typeId`, `description`), 403 means the account
   *   cannot manage resources, 409 the plan's resource limit is reached.
   */
  async register(dto: RegisterResource): Promise<Resource> {
    const { data } = await apiClient.post<Resource>("/resources", dto);
    return data;
  },
};
