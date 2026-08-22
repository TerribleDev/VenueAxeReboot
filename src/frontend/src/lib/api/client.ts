import { client } from './generated/client.gen';

// Configure the generated client to point to our ASP.NET Core MVC API with cookie authentication
client.setConfig({
    baseUrl: 'http://localhost:5280',
    credentials: 'include'
});

export * from './generated';
export { client };
