(function () {
  // Start a SignalR connection for presence and exposes a promise
  if (window.playersPresenceConnectionPromise) return;

  const conn = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/players")
    .withAutomaticReconnect()
    .build();

  window.playersPresenceConnection = conn;

  window.playersPresenceConnectionPromise = conn
    .start()
    .then(() => {
      console.debug("[players-presence-bootstrap] connection started");
      return conn;
    })
    .catch((err) => {
      console.error(
        "[players-presence-bootstrap] connection start failed",
        err
      );
      return Promise.reject(err);
    });
})();
