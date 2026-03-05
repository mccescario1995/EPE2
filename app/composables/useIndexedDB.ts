// composables/useIndexedDB.ts
export function useIndexedDBWorker() {
  const worker = new Worker(new URL("../workers/indexedDBWorker.ts", import.meta.url), {
    type: "module",
  });

  const sendMessage = <T>(action: string, payload?: any): Promise<T> => {
    return new Promise((resolve, reject) => {
      worker.onmessage = (event: MessageEvent) => {
        const { success, error, data } = event.data;
        if (success) resolve(data);
        else reject(error);
      };
      worker.postMessage({ action, payload });
    });
  };

  return {
    put: (item: any) => sendMessage("put", item),
    bulkPut: (items: any[]) => sendMessage("bulkPut", items),
    get: (id: string | number) => sendMessage<any>("get", id),
    getAll: () => sendMessage<any[]>("getAll"),
    getAllPaginated: (offset: number, limit: number) =>
      sendMessage<any[]>("getAllPaginated", { offset, limit }),
    queryByIndex: (indexName: string, value: any) =>
      sendMessage<any[]>("queryByIndex", { indexName, value }),
    remove: (id: string | number) => sendMessage("remove", id),
    removeByIndex: (indexName: string, value: any) =>
      sendMessage("removeByIndex", { indexName, value }),
    clear: () => sendMessage("clear"),
  };
}
