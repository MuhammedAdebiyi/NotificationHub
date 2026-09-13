<?php

declare(strict_types=1);

namespace NotificationHub\SDK;

class NotificationHubException extends \RuntimeException
{
    private int $statusCode;

    public function __construct(string $message, int $statusCode = 0)
    {
        parent::__construct($message);
        $this->statusCode = $statusCode;
    }

    public function getStatusCode(): int
    {
        return $this->statusCode;
    }
}
