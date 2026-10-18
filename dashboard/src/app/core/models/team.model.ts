export interface Team {
  id: string;
  name: string;
  description: string;
  memberIds: string[];
  createdAt: Date;
}

export interface CreateTeamDto {
  name: string;
  description: string;
  memberIds: string[];
}