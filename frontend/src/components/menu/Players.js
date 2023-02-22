export function Players({ players, handleClick }) {
  return (
    <div>
      {players.map((p) => (
        <button key={p} onClick={() => handleClick(p)}>
          Player: {p}
        </button>
      ))}
    </div>
  );
}
