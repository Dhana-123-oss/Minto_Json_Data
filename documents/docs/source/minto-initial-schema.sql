CREATE TABLE products (id uuid PRIMARY KEY, name text NOT NULL, category text NOT NULL, price numeric(12,2) NOT NULL, is_active boolean NOT NULL);
CREATE TABLE inventory (product_id uuid PRIMARY KEY REFERENCES products(id), available integer NOT NULL, reserved integer NOT NULL, version bigint NOT NULL DEFAULT 0);
CREATE TABLE orders (id uuid PRIMARY KEY, customer_id text NOT NULL, address text NOT NULL, status integer NOT NULL, payment_method text NOT NULL, payment_completed boolean NOT NULL, total numeric(12,2) NOT NULL, created_at timestamptz NOT NULL, rider_id text, pickup_code text, idempotency_key text UNIQUE);
CREATE TABLE order_lines (id uuid PRIMARY KEY, order_id uuid NOT NULL REFERENCES orders(id), product_id uuid NOT NULL REFERENCES products(id), product_name text NOT NULL, quantity integer NOT NULL, unit_price numeric(12,2) NOT NULL);
CREATE TABLE carts (id uuid PRIMARY KEY, customer_id text NOT NULL UNIQUE, updated_at timestamptz NOT NULL);
CREATE TABLE cart_lines (id uuid PRIMARY KEY, cart_id uuid NOT NULL REFERENCES carts(id) ON DELETE CASCADE, product_id uuid NOT NULL REFERENCES products(id), quantity integer NOT NULL, UNIQUE(cart_id, product_id));
CREATE TABLE outbox (id uuid PRIMARY KEY, event_type text NOT NULL, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL, published boolean NOT NULL DEFAULT false);
