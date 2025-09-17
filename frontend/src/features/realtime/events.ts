export type RealtimeEvents = {
  bookCreated: { id: string };
  bookUpdated: { id: string };
  bookDeleted: { id: string };
  bookFavorited: { id: string; userId?: string };
  bookUnfavorited: { id: string; userId?: string };
  bookRead: { id: string; userId?: string };
  statsUpdated: undefined;
};


