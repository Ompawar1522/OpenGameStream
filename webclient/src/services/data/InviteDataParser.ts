import type { InviteData } from "../clients/ClientInviteData";

export function ParseInviteData(code: string) : InviteData{
    const json = JSON.parse(atob(code));
    return json as InviteData;
}