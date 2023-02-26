export function Players({ players, handleClick }) {
  return (
    <div>
      <h1>Active players</h1>
      {players.map((p) => (
        <button key={p} onClick={() => handleClick(p)}>
          Player: {p}
        </button>
      ))}
    </div>
  );
}
