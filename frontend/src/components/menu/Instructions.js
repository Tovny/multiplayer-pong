export function Instructions({ onClick, visible }) {
  return visible ? (
    <div>
      <h3 style={{ textAlign: "center", marginBottom: 50 }}>
        Use ↑ and ↓ arrows to move the paddle.
      </h3>
      <button onClick={onClick} className="btn">
        got it
      </button>
    </div>
  ) : null;
}
