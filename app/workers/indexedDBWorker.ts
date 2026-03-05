// workers/indexedDBWorker.ts
// IndexedDB CRUD + pagination + delete by index

const DB_NAME = "MyDB";
const STORE_NAME = "records";

let db: IDBDatabase | null = null;

function openDB(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    if (db) return resolve(db);

    const request = indexedDB.open(DB_NAME, 1);

    request.onupgradeneeded = (e: IDBVersionChangeEvent) => {
      const database = (e.target as IDBOpenDBRequest).result;
      if (!database.objectStoreNames.contains(STORE_NAME)) {
        const store = database.createObjectStore(STORE_NAME, { keyPath: "id" });
        store.createIndex("by_date", "date", { unique: false });
        store.createIndex("by_name", "name", { unique: false });
      }
    };

    request.onsuccess = () => {
      db = request.result;
      resolve(db);
    };

    request.onerror = () => reject(request.error);
  });
}

self.onmessage = async (event: MessageEvent) => {
  const { action, payload } = event.data;

  try {
    const database = await openDB();

    // CREATE / UPDATE
    if (action === "put") {
      const tx = database.transaction(STORE_NAME, "readwrite");
      tx.objectStore(STORE_NAME).put(payload);
      tx.oncomplete = () => (self as any).postMessage({ action, success: true });
    }

    // BULK INSERT
    if (action === "bulkPut") {
      const tx = database.transaction(STORE_NAME, "readwrite");
      const store = tx.objectStore(STORE_NAME);
      for (const item of payload) store.put(item);
      tx.oncomplete = () => (self as any).postMessage({ action, success: true });
    }

    // READ ONE
    if (action === "get") {
      const tx = database.transaction(STORE_NAME, "readonly");
      const req = tx.objectStore(STORE_NAME).get(payload);
      req.onsuccess = () =>
        (self as any).postMessage({ action, success: true, data: req.result });
    }

    // READ ALL
    if (action === "getAll") {
      const tx = database.transaction(STORE_NAME, "readonly");
      const req = tx.objectStore(STORE_NAME).getAll();
      req.onsuccess = () =>
        (self as any).postMessage({ action, success: true, data: req.result });
    }

    // PAGINATED READ
    if (action === "getAllPaginated") {
      const { offset, limit } = payload;
      const tx = database.transaction(STORE_NAME, "readonly");
      const store = tx.objectStore(STORE_NAME);
      const req = store.openCursor();
      const results: any[] = [];
      let skipped = 0;

      req.onsuccess = () => {
        const cursor = req.result;
        if (cursor) {
          if (skipped >= offset && results.length < limit) {
            results.push(cursor.value);
          }
          skipped++;
          cursor.continue();
        } else {
          (self as any).postMessage({ action, success: true, data: results });
        }
      };
    }

    // QUERY BY INDEX
    if (action === "queryByIndex") {
      const { indexName, value } = payload;
      const tx = database.transaction(STORE_NAME, "readonly");
      const req = tx.objectStore(STORE_NAME).index(indexName).getAll(value);
      req.onsuccess = () =>
        (self as any).postMessage({ action, success: true, data: req.result });
    }

    // DELETE ONE (by id)
    if (action === "remove") {
      const tx = database.transaction(STORE_NAME, "readwrite");
      tx.objectStore(STORE_NAME).delete(payload);
      tx.oncomplete = () => (self as any).postMessage({ action, success: true });
    }

    // DELETE BY INDEX
    if (action === "removeByIndex") {
      const { indexName, value } = payload;
      const tx = database.transaction(STORE_NAME, "readwrite");
      const store = tx.objectStore(STORE_NAME);
      const index = store.index(indexName);
      const req = index.openCursor(IDBKeyRange.only(value));

      req.onsuccess = () => {
        const cursor = req.result;
        if (cursor) {
          cursor.delete(); // delete this entry
          cursor.continue(); // keep going
        } else {
          (self as any).postMessage({ action, success: true });
        }
      };
    }

    // CLEAR STORE
    if (action === "clear") {
      const tx = database.transaction(STORE_NAME, "readwrite");
      tx.objectStore(STORE_NAME).clear();
      tx.oncomplete = () => (self as any).postMessage({ action, success: true });
    }
  } catch (err) {
    (self as any).postMessage({ action, error: err });
  }
};
