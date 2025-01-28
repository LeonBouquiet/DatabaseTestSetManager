
set identity_insert dbo.Product on 

insert into dbo.Product (Id, Code, [Name])
values 
	(1,		'ANV',	'Anvil')		,
	(2,		'HG',	'Hen Grenade'),
	(3,		'BS',	'Bird Seed'),
	(4,		'GLU',	'Glue'),
	(5,		'JM',	'Jet Motor')

set identity_insert dbo.Product off
go
