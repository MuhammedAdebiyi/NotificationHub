export const ROLES = {
  OWNER: 'owner',
  ADMIN: 'admin',
  MEMBER: 'member',
  REVOKED: 'revoked',
} as const

export type Role = (typeof ROLES)[keyof typeof ROLES]

export function canManageMembers(role: string): boolean {
  return role === ROLES.OWNER || role === ROLES.ADMIN
}
