self.addEventListener("push", (event) => {
  const data = event.data ? event.data.json() : { title: "SevaSetu", body: "A care update is waiting." };
  event.waitUntil(self.registration.showNotification(data.title || "SevaSetu", { body: data.body || "" }));
});
