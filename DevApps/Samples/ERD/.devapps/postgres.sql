DROP TABLE IF EXISTS contacts;
DROP TABLE IF EXISTS customers;
CREATE TABLE customers(
   customer_id INT GENERATED ALWAYS AS IDENTITY,
   customer_name VARCHAR(255) NOT NULL,
   PRIMARY KEY(customer_id)
);
CREATE TABLE contacts(
   contact_id INT GENERATED ALWAYS AS IDENTITY,
   customer_id INT,
   contact_name VARCHAR(255) NOT NULL,
   phone VARCHAR(15),
   email VARCHAR(100),
   PRIMARY KEY(contact_id),
   CONSTRAINT fk_customer
      FOREIGN KEY(customer_id)
	  REFERENCES customers(customer_id)
	  ON DELETE CASCADE
);
INSERT INTO customers(customer_name)
VALUES('BlueBird Inc'),
      ('Dolphin LLC');
INSERT INTO contacts(customer_id, contact_name, phone, email)
VALUES(1,'John Doe','(408)-111-1234','[[email protected]](../cdn-cgi/l/email-protection.html)'),
      (1,'Jane Doe','(408)-111-1235','[[email protected]](../cdn-cgi/l/email-protection.html)'),
      (2,'David Wright','(408)-222-1234','[[email protected]](../cdn-cgi/l/email-protection.html)');